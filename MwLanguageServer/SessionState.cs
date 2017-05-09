using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using JsonRpc.Standard.Client;
using JsonRpc.Standard.Contracts;
using JsonRpc.Standard.Server;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Contracts.Client;
using LanguageServer.VsCode.Server;
using MwLanguageServer.Linter;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace MwLanguageServer
{
    public class SessionState
    {

        public SessionState(ClientProxy clientProxy)
        {
            if (clientProxy == null) throw new ArgumentNullException(nameof(clientProxy));
            ClientProxy = clientProxy;
            DocumentStates = new ConcurrentDictionary<Uri, DocumentState>();
            WikitextLinter = new WikitextLinter(new WikitextParser());
        }

        public ClientProxy ClientProxy { get; }
        
        public ConcurrentDictionary<Uri, DocumentState> DocumentStates { get; }

        public LanguageServerSettings Settings { get; set; } = new LanguageServerSettings();

        public WikitextLinter WikitextLinter { get; }

        public void Attach(DocumentState doc)
        {
            doc.DocumentLinted += DocumentState_DocumentLinted;
        }

        public bool RemoveDocument(Uri documentUri)
        {
            if (DocumentStates.TryRemove(documentUri, out var doc))
            {
                Detach(doc);
                return true;
            }
            return false;
        }

        public void Detach(DocumentState doc)
        {
            doc.DocumentLinted -= DocumentState_DocumentLinted;
        }

        private void DocumentState_DocumentLinted(object sender, EventArgs args)
        {
            var d = (DocumentState)sender;
            ClientProxy.TextDocument.PublishDiagnostics(d.TextDocument.Uri, d.LintedDocument.Diagnostics);
        }

    }

    public class SessionStateManager
    {

        public SessionStateManager(IComponentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Context = context;
        }

        private readonly ConcurrentDictionary<string, SessionState> stateDict =
            new ConcurrentDictionary<string, SessionState>();

        public IComponentContext Context { get; }

        /// <summary>
        /// Gets or creates a <see cref="SessionState"/> for the specified session.
        /// </summary>
        public SessionState GetState(ISession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            return stateDict.GetOrAdd(session.Id, k => Context.Resolve<SessionState>());
        }
    }

    /// <summary>
    /// Mutable document state.
    /// </summary>
    public class DocumentState
    {

        public DocumentState(TextDocument doc, WikitextLinter linter)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (linter == null) throw new ArgumentNullException(nameof(linter));
            TextDocument = doc;
            WikitextLinter = linter;
            DocumentLinter = new TextDocumentLinter(this);
            Synchronizer = new TextDocumentSynchronizer(this);
            DocumentLinter = new TextDocumentLinter(this);
            // Caller's responsibility.
            // DocumentLinter.RequestLint();
        }

        public event EventHandler DocumentChanged;

        public event EventHandler DocumentLinted;

        public LintedWikitextDocument LintedDocument { get; private set; }

        public TextDocument TextDocument { get; private set; }

        private readonly TextDocumentSynchronizer Synchronizer;

        private readonly TextDocumentLinter DocumentLinter;     // A WikitextLinter with delay

        private readonly WikitextLinter WikitextLinter;

        public void NotifyChanges(IEnumerable<TextDocumentContentChangeEvent> changes)
        {
            if (changes == null) return;
            Synchronizer.NotifyChanges(changes);
        }

        public void RequestLint()
        {
            DocumentLinter.RequestLint();
        }

        protected virtual void OnDocumentChanged()
        {
            DocumentChanged?.Invoke(this, EventArgs.Empty);
            // Update the AST, btw.
            DocumentLinter.RequestLint();
        }

        protected virtual void OnDocumentLinted()
        {
            DocumentLinted?.Invoke(this, EventArgs.Empty);
        }

        private class TextDocumentSynchronizer
        {
            /// <summary>
            /// Actually makes the changes to Owner.TextDocument per this milliseconds.
            /// </summary>
            private const int RenderChangesDelay = 100;

            public TextDocumentSynchronizer(DocumentState owner)
            {
                if (owner == null) throw new ArgumentNullException(nameof(owner));
                Owner = owner;
            }

            public DocumentState Owner { get; }

            private List<TextDocumentContentChangeEvent> impendingChanges;

            private readonly object syncLock = new object();

            private readonly object makeChangesLock = new object();

            private int willMakeChanges = 0;

            public void NotifyChanges(IEnumerable<TextDocumentContentChangeEvent> changes)
            {
                if (changes == null) throw new ArgumentNullException(nameof(changes));
                if (changes is ICollection<TextDocumentContentChangeEvent> col && col.Count == 0) return;
                lock (syncLock)
                {
                    if (impendingChanges == null)
                        impendingChanges = changes.ToList();
                    else
                        impendingChanges.AddRange(changes);
                }
                if (Interlocked.Exchange(ref willMakeChanges, 1) == 0)
                {
                    // Note: If we're currently in TryMakeChanges, willMakeChanges should be 0
                    Task.Delay(RenderChangesDelay).ContinueWith(t => Task.Run((Action) TryMakeChanges));
                }
            }

            // Only 1 TryMakeChanges() running at one time.
            private void TryMakeChanges()
            {
                if (!Monitor.TryEnter(makeChangesLock)) return;
                try
                {
                    Interlocked.Exchange(ref willMakeChanges, 0);
                    while (true)
                    {
                        List<TextDocumentContentChangeEvent> localChanges;
                        // Pick up the changes.
                        lock (syncLock)
                        {
                            localChanges = impendingChanges;
                            if (localChanges == null || localChanges.Count == 0) return;
                            impendingChanges = null;
                        }
                        // Make the changes.
                        var doc = Owner.TextDocument.ApplyChanges(localChanges);
                        Owner.TextDocument = doc;
                        // We have done the changes.
                        if (impendingChanges == null)
                        {
                            localChanges.Clear();
                            lock (syncLock)
                            {
                                if (impendingChanges == null)
                                    impendingChanges = localChanges;
                            }
                        }
                        Owner.OnDocumentChanged();
                    }
                }
                finally
                {
                    Monitor.Exit(makeChangesLock);
                }
            }
        }

        private class TextDocumentLinter
        {
            /// <summary>
            /// Actually lint the document per this milliseconds.
            /// </summary>
            private const int RenderChangesDelay = 100;

            public TextDocumentLinter(DocumentState owner)
            {
                if (owner == null) throw new ArgumentNullException(nameof(owner));
                Owner = owner;
            }

            public DocumentState Owner { get; }

            private readonly object lintLock = new object();

            private int willLint = 0;

            private int impendingRequests = 0;

            /// <summary>
            /// Request for linting the document, without any condition.
            /// </summary>
            public void RequestLint()
            {
                Interlocked.Increment(ref impendingRequests);
                if (Interlocked.Exchange(ref willLint, 1) == 0)
                {
                    Task.Delay(RenderChangesDelay).ContinueWith(t => Task.Run((Action) TryLint));
                }
            }

            private void TryLint()
            {
                if (!Monitor.TryEnter(lintLock)) return;
                try
                {
                    while (Interlocked.Exchange(ref impendingRequests, 0) > 0)
                    {
                        var doc = Owner.TextDocument;
                        var linted = Owner.WikitextLinter.Lint(doc);
                        // document has been changed!
                        // then just wait for another RequestLint()
                        if (doc != Owner.TextDocument) continue;
                        Owner.LintedDocument = linted;
                        Owner.OnDocumentLinted();
                    }
                }
                finally
                {
                    Monitor.Exit(lintLock);
                }
            }
        }
    }
}