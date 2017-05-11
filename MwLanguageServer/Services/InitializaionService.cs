using System;
using System.Threading.Tasks;
using JsonRpc.Standard;
using JsonRpc.Standard.Contracts;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Contracts.Client;
using Newtonsoft.Json.Linq;

namespace MwLanguageServer.Services
{
    public class InitializaionService : LanguageServiceBase
    {
        private readonly ClientProxy client;
        private readonly ServiceHostLifetime lifetime;

        public InitializaionService(ClientProxy client, ServiceHostLifetime lifetime)
        {
            this.client = client;
            this.lifetime = lifetime;
        }

        [JsonRpcMethod(AllowExtensionData = true)]
        public InitializeResult Initialize(int processId, Uri rootUri, ClientCapabilities capabilities,
            JToken initializationOptions = null, string trace = null)
        {
            return new InitializeResult(new ServerCapabilities
            {
                HoverProvider = true,
                SignatureHelpProvider = new SignatureHelpOptions("{|=}\n"),
                CompletionProvider = new CompletionOptions(true, "{[|"),
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    WillSave = true,
                    Change = TextDocumentSyncKind.Incremental
                }
            });
        }

        [JsonRpcMethod(IsNotification = true)]
        public void Initialized()
        {

        }

        [JsonRpcMethod]
        public void Shutdown()
        {

        }

        [JsonRpcMethod(IsNotification = true)]
        public void Exit()
        {
            lifetime.Stop();
        }

        [JsonRpcMethod("$/cancelRequest", IsNotification = true)]
        public void CancelRequest(MessageId id)
        {
            RequestContext.ServiceHost.TryCancelRequest(id);
        }
    }
}
