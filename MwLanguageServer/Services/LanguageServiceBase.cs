using JsonRpc.Standard.Server;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;

namespace MwLanguageServer.Services
{
    public class LanguageServiceBase : JsonRpcService
    {

        protected LanguageServerSession Session => (LanguageServerSession)RequestContext.Session;

        protected TextDocumentCollection Documents => Session.Documents;

    }
}
