using System;
using System.Threading;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;

namespace MwLanguageServer
{
    public class LanguageServerSession : Session
    {

        public LanguageServerSession(IJsonRpcContractResolver contractResolver)
        {
            Documents = new TextDocumentCollection();
            DiagnosticProvider = new DiagnosticProvider(Documents);
        }

        public TextDocumentCollection Documents { get; }

        public DiagnosticProvider DiagnosticProvider { get; }

        public LanguageServerSettings Settings { get; set; } = new LanguageServerSettings();
        
    }
}