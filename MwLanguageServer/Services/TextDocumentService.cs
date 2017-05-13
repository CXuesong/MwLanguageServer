using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using LanguageServer.VsCode;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MwLanguageServer.Services
{
    [JsonRpcScope(MethodPrefix = "textDocument/")]
    public class TextDocumentService : LanguageServiceBase
    {
        private readonly ClientProxy client;
        private readonly ILoggerFactory loggerFactory;

        public TextDocumentService(ClientProxy client, ILoggerFactory loggerFactory)
        {
            this.client = client;
            this.loggerFactory = loggerFactory;
        }

        [JsonRpcMethod]
        public Hover Hover(TextDocumentIdentifier textDocument, Position position)
        {
            var doc = Session.DocumentStates[textDocument.Uri];
            return doc.LintedDocument.GetHover(position);
        }

        [JsonRpcMethod]
        public async Task<SignatureHelp> SignatureHelp(TextDocumentIdentifier textDocument, Position position,
            CancellationToken ct)
        {
            var doc = Session.DocumentStates[textDocument.Uri];
            await doc.AnalyzeAsync(ct);
            ct.ThrowIfCancellationRequested();
            var sh = doc.LintedDocument.GetSignatureHelp(position, Session.MagicTemplateInfoStore,
                Session.PageInfoStore);
            return sh;
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidOpen(TextDocumentItem textDocument)
        {
            var doc = new DocumentState(TextDocument.Load<FullTextDocument>(textDocument), loggerFactory);
            Session.DocumentStates[textDocument.Uri] = doc;
            Session.Attach(doc);
            doc.RequestAnalysis();
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidChange(TextDocumentIdentifier textDocument,
            ICollection<TextDocumentContentChangeEvent> contentChanges)
        {
            Session.DocumentStates[textDocument.Uri].NotifyChanges(contentChanges);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void WillSave(TextDocumentIdentifier textDocument, TextDocumentSaveReason reason)
        {

        }

        [JsonRpcMethod(IsNotification = true)]
        public async Task DidClose(TextDocumentIdentifier textDocument)
        {
            Session.RemoveDocument(textDocument.Uri);
            if (textDocument.Uri.IsUntitled())
            {
                await Session.ClientProxy.Document.PublishDiagnostics(textDocument.Uri, Diagnostic.EmptyDiagnostics);
            }
        }

        private static readonly Regex leftBracketMatcher =
            new Regex(@"((?<!\{)\{\{\{?|(?<!\[)\[\[)(?=[^\r\n\|\}\]]*\B)", RegexOptions.RightToLeft);

        [JsonRpcMethod]
        public async Task<CompletionList> Completion(TextDocumentIdentifier textDocument, Position position,
            CancellationToken ct)
        {
            var doc = Session.DocumentStates[textDocument.Uri];
            await doc.AnalyzeAsync(ct);
            return new CompletionList(true,
                doc.LintedDocument.GetCompletionItems(position,
                    Session.MagicTemplateInfoStore,
                    Session.PageInfoStore));
        }

    }
}
