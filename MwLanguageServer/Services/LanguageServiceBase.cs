using System;
using JsonRpc.Standard.Server;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;

namespace MwLanguageServer.Services
{
    public class LanguageServiceBase : JsonRpcService
    {

        protected SessionState Session => RequestContext.Features.Get<ISessionStateFeature>().State;
    }
}
