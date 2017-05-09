using System;
using JsonRpc.Standard.Server;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;

namespace MwLanguageServer.Services
{
    public class LanguageServiceBase : JsonRpcService
    {
        internal SessionStateManager StateManager { get; set; }

        protected SessionState Session => StateManager.GetState(RequestContext.Session);
    }
}
