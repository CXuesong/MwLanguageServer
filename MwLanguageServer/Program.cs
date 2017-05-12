using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Autofac;
using JsonRpc.Standard;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Dataflow;
using JsonRpc.Standard.Server;
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

        public static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            ConfigureContainer(builder);
            using (var container = builder.Build())
            {
#if DEBUG
                if (container.Resolve<ApplicationConfiguration>().WaitForDebugger)
                {
                    while (!Debugger.IsAttached) Thread.Sleep(1000);
                    Debugger.Break();
                }
#endif
                var logger = container.Resolve<ILoggerFactory>().CreateLogger("Application");
                logger.LogInformation("Start logging.");
                logger.LogInformation("Arguments: {arguments}", (object) args);
                var serviceHost = container.Resolve<IJsonRpcServiceHost>();
                var lifetime = container.Resolve<ServiceHostLifetime>();
                using (lifetime.CancellationToken.Register(
                    () => container.Resolve<ConsoleIoService>().ConsoleMessageSource.Complete()))
                {
                    lifetime.CancellationToken.WaitHandle.WaitOne();
                }
                logger.LogInformation("Stop logging.");
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
                            ["--manual"] = "Application:Manual",
                            ["--waitForDebugger"] = "Application:WaitForDebugger",
                        })
                    .Build())
                .SingleInstance();
            builder.Register(ctx => ctx.Resolve<IConfiguration>()
                .GetSection("Application")
                .Get<ApplicationConfiguration>());
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
            builder.RegisterType<SessionStateManager>().SingleInstance();
            builder.RegisterType<SessionState>();
            // JSON RPC Services
            foreach (var t in typeof(Program).GetTypeInfo()
                .Assembly.ExportedTypes
                .Where(t => t.IsAssignableTo<JsonRpcService>()))
            {
                var rb = builder.RegisterType(t);
                if (t.IsAssignableTo<LanguageServiceBase>())
                    rb.OnActivated(e => ((LanguageServiceBase) e.Instance).StateManager =
                        e.Context.Resolve<SessionStateManager>());
            }
        }

        private static JsonRpcClient RpcClientFactory(IComponentContext context)
        {
            var cio = context.Resolve<ConsoleIoService>();
            var client = new JsonRpcClient();
            client.Attach(cio.ConsoleMessageSource, cio.ConsoleMessageTarget);
            if (context.Resolve<ApplicationConfiguration>().Debug)
            {
                var logger = context.Resolve<ILoggerFactory>().CreateLogger("CLIENT");
                client.MessageSending += (_, e) =>
                {
                    logger.LogTrace("< {@Request}", e.Message);
                };
                client.MessageReceiving += (_, e) =>
                {
                    logger.LogTrace("> {@Response}", e.Message);
                };
            }
            return client;
        }

        private static IJsonRpcServiceHost ServiceHostFactory(IComponentContext context)
        {
            var builder = new ServiceHostBuilder
            {
                ContractResolver = myContractResolver,
                Session = new Session(),
                Options = JsonRpcServiceHostOptions.ConsistentResponseSequence,
                LoggerFactory = context.Resolve<ILoggerFactory>(),
                ServiceFactory = new AutofacServiceFactory(context.Resolve<ILifetimeScope>())
            };
            builder.Register(typeof(Program).GetTypeInfo().Assembly);
            builder.UseCancellationHandling();
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
            var host = builder.Build();
            var cio = context.Resolve<ConsoleIoService>();
            host.Attach(cio.ConsoleMessageSource, cio.ConsoleMessageTarget);
            return host;
        }
    }
}