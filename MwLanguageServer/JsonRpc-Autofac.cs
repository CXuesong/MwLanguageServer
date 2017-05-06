using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Autofac;
using JsonRpc.Standard.Server;

namespace MwLanguageServer
{
    public class AutofacServiceFactory : IServiceFactory
    {

        private readonly ConcurrentDictionary<IJsonRpcService, ILifetimeScope> scopeDict = new ConcurrentDictionary<IJsonRpcService, ILifetimeScope>();

        public AutofacServiceFactory(ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            Scope = scope;
        }

        public ILifetimeScope Scope { get; }

        /// <inheritdoc />
        public IJsonRpcService CreateService(Type serviceType, RequestContext context)
        {
            var scope = Scope.BeginLifetimeScope();
            IJsonRpcService instance;
            try
            {
                instance = (IJsonRpcService) Scope.Resolve(serviceType);
                scopeDict.TryAdd(instance, scope);
            }
            catch (Exception)
            {
                scope.Dispose();
                throw;
            }
            instance.RequestContext = context;
            return instance;
        }

        /// <inheritdoc />
        public void ReleaseService(IJsonRpcService service)
        {
            service.RequestContext = null;
            // Let autofac take care of the disposal
            if (scopeDict.TryRemove(service, out var subScope))
            {
                subScope.Dispose();
            }
        }
    }
}
