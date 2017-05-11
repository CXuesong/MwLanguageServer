using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using LanguageServer.VsCode;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;
using Microsoft.Extensions.Logging;

namespace MwLanguageServer.Services
{
    [JsonRpcScope(MethodPrefix = "textDocument/")]
    public class TextDocumentService : LanguageServiceBase
    {
        private readonly ILoggerFactory loggerFactory;

        public TextDocumentService(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        [JsonRpcMethod]
        public Hover Hover(TextDocumentIdentifier textDocument, Position position)
        {
            var doc = Session.DocumentStates[textDocument.Uri];
            return doc.LintedDocument.GetHover(position);
        }

        [JsonRpcMethod]
        public SignatureHelp SignatureHelp(TextDocumentIdentifier textDocument, Position position)
        {
            var doc = Session.DocumentStates[textDocument.Uri];
            return doc.LintedDocument.GetSignatureHelp(position, Session.PageInfoStore);
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidOpen(TextDocumentItem textDocument)
        {
            var doc = new DocumentState(TextDocument.Load<FullTextDocument>(textDocument), loggerFactory);
            Session.DocumentStates[textDocument.Uri] = doc;
            Session.Attach(doc);
            doc.RequestLint();
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

        private static readonly CompletionItem[] PredefinedCompletionItems =
        {
            new CompletionItem(".NET", CompletionItemKind.Keyword,
                "Keyword1",
                "Short for **.NET Framework**, a software framework by Microsoft (possibly its subsets) or later open source .NET Core.",
                null),
            new CompletionItem(".NET Standard", CompletionItemKind.Keyword,
                "Keyword2",
                "The .NET Standard is a formal specification of .NET APIs that are intended to be available on all .NET runtimes.",
                null),
            new CompletionItem(".NET Framework", CompletionItemKind.Keyword,
                "Keyword3",
                ".NET Framework (pronounced dot net) is a software framework developed by Microsoft that runs primarily on Microsoft Windows.", null),
        };

        [JsonRpcMethod]
        public CompletionList Completion(TextDocumentIdentifier textDocument, Position position)
        {
            return new CompletionList(PredefinedCompletionItems);
        }

    }
}
