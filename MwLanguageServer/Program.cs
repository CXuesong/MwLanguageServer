using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Autofac;
using JsonRpc.DynamicProxy.Client;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using JsonRpc.Streams;
using LanguageServer.VsCode;
using LanguageServer.VsCode.Contracts.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MwLanguageServer.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ISerilogLogger = Serilog.ILogger;

namespace MwLanguageServer
{
    internal static class Program
    {
        private static readonly IJsonRpcContractResolver myContractResolver = new JsonRpcContractResolver
        {
            // Use camelcase for RPC method names.
            NamingStrategy = new CamelCaseJsonRpcNamingStrategy(),
            // Use camelcase for the property names in parameter value objects
            ParameterValueConverter = new CamelCaseJsonValueConverter()
        };

        public static int Main(string[] args)
        {
            var builder = new ContainerBuilder();
            ConfigureContainer(builder);
            using (var container = builder.Build())
            {
                var config = container.Resolve<ApplicationConfiguration>();
#if DEBUG
                if (config.WaitForDebugger)
                {
                    while (!Debugger.IsAttached) Thread.Sleep(1000);
                    Debugger.Break();
                }
#endif
                if (!string.IsNullOrEmpty(config.Language))
                {
                    CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture =
                        new CultureInfo(config.Language);
                }
                var logger = container.Resolve<ILoggerFactory>().CreateLogger("Application");
                logger.LogInformation("Start logging.");
                logger.LogInformation("Arguments: {arguments}", (object) args);
                var result = StartServerHandler(container);
                logger.LogInformation("Exit. Code = {code}.", result);
                return result;
            }
        }

        private static void ConfigureContainer(ContainerBuilder builder)
        {
            // Logging
            builder.Register(ctx =>
                {
                    var writer =
                        File.CreateText("MwLanguageServer-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
                    writer.AutoFlush = true;
                    return writer;
                })
                .Named<TextWriter>("LoggerTextWriter");
            builder.Register<ISerilogLogger>(ctx => new LoggerConfiguration()
                    .Destructure.AsScalar<RequestMessage>()
                    .Destructure.AsScalar<ResponseMessage>()
                    .MinimumLevel.Is(ctx.Resolve<ApplicationConfiguration>().Verbose
                        ? LogEventLevel.Verbose
                        : LogEventLevel.Debug)
                    .WriteTo
                    .TextWriter(new SynchronizedTextWriter(ctx.ResolveNamed<TextWriter>("LoggerTextWriter")),
                        outputTemplate:
                        "{Timestamp:HH:mm:ss} [{Level}] {SourceContext} {Message}{NewLine}{Exception}")
#if DEBUG
                    .WriteTo.Trace(outputTemplate:
                        "{Timestamp:HH:mm:ss} [{Level}] {SourceContext} {Message}{NewLine}{Exception}")
#endif
                    .CreateLogger())
                .SingleInstance();
            builder.Register<ILoggerFactory>(ctx =>
                {
                    var lf = new LoggerFactory();
                    lf.AddSerilog(ctx.Resolve<ISerilogLogger>());
                    return lf;
                }
            );
            // Configuration
            builder.Register<IConfiguration>(ctx => new ConfigurationBuilder().AddJsonFile("config.json")
                    .AddCommandLine(Utility.ProcessCommandlineArguments(Environment.GetCommandLineArgs().Skip(1)),
                        new Dictionary<string, string>
                        {
                            ["--debug"] = "Application:Debug",
                            ["--verbose"] = "Application:Verbose",
                            ["--manual"] = "Application:Manual",
                            ["--waitForDebugger"] = "Application:WaitForDebugger",
                        })
                    .Build())
                .SingleInstance();
            builder.Register(ctx => ctx.Resolve<IConfiguration>()
                .GetSection("Application")
                .Get<ApplicationConfiguration>());
            // JSON RPC Session
            builder.RegisterType<SessionState>();
            // JSON RPC Client
            builder.Register(ctx => new ConsoleIoService(ctx.Resolve<ApplicationConfiguration>().Manual))
                .SingleInstance();
            builder.Register(RpcClientFactory).SingleInstance();
            builder.Register(ctx => new JsonRpcProxyBuilder {ContractResolver = myContractResolver})
                .SingleInstance();
            builder.RegisterType<ClientProxy>().SingleInstance();
            // JSON RPC Server
            builder.RegisterType<ServiceHostLifetime>().SingleInstance();
            builder.Register(ServiceHostFactory).SingleInstance();
            // JSON RPC Services
            foreach (var t in typeof(Program).GetTypeInfo()
                .Assembly.ExportedTypes
                .Where(t => t.IsAssignableTo<JsonRpcService>()))
            {
                builder.RegisterType(t);
            }
        }

        private static JsonRpcClient RpcClientFactory(IComponentContext context)
        {
            var cio = context.Resolve<ConsoleIoService>();
            var handler = new StreamRpcClientHandler();
            var disposable = handler.Attach(cio.ConsoleMessageReader, cio.ConsoleMessageWriter);
            var client = new JsonRpcClient(handler);
            if (context.Resolve<ApplicationConfiguration>().Debug)
            {
                var logger = context.Resolve<ILoggerFactory>().CreateLogger("CLIENT");
                handler.MessageSending += (_, e) =>
                {
                    logger.LogTrace("< {@Request}", e.Message);
                };
                handler.MessageReceiving += (_, e) =>
                {
                    logger.LogTrace("> {@Response}", e.Message);
                };
            }
            return client;
        }

        private static IJsonRpcServiceHost ServiceHostFactory(IComponentContext context)
        {
            var builder = new JsonRpcServiceHostBuilder
            {
                ContractResolver = myContractResolver,
                LoggerFactory = context.Resolve<ILoggerFactory>(),
                ServiceFactory = new AutofacServiceFactory(context.Resolve<ILifetimeScope>())
            };
            builder.Register(typeof(Program).GetTypeInfo().Assembly);
            if (context.Resolve<ApplicationConfiguration>().Debug)
            {
                var logger = context.Resolve<ILoggerFactory>().CreateLogger("SERVER");
                // Log all the client-to-server calls.
                builder.Intercept(async (ctx, next) =>
                {
                    logger.LogTrace("> {@Request}", ctx.Request);
                    await next();
                    logger.LogTrace("< {@Response}", ctx.Response);
                });
            }
            builder.UseCancellationHandling();
            return builder.Build();
        }

        private static int StartServerHandler(IComponentContext context)
        {
            var serviceHost = context.Resolve<IJsonRpcServiceHost>();
            var handler = new StreamRpcServerHandler(serviceHost,
                StreamRpcServerHandlerOptions.ConsistentResponseSequence |
                StreamRpcServerHandlerOptions.SupportsRequestCancellation);
            var state = context.Resolve<SessionState>();
            handler.DefaultFeatures.Set<ISessionStateFeature>(new SessionStateFeature(state));
            var cio = context.Resolve<ConsoleIoService>();
            using (handler.Attach(cio.ConsoleMessageReader, cio.ConsoleMessageWriter))
            {
                var lifetime = context.Resolve<ServiceHostLifetime>();
                lifetime.CancellationToken.WaitHandle.WaitOne();
            }
            return 0;
        }
    }
}