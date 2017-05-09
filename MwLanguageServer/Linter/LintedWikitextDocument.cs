using System;
using System.Collections.Generic;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwParserFromScratch.Nodes;

namespace MwLanguageServer.Linter
{
    public class LintedWikitextDocument
    {

        private static readonly Diagnostic[] EmptyDiagnostics = { };

        // We need to assume the content of _Root is readonly.
        private readonly Wikitext _Root;

        public LintedWikitextDocument(Wikitext root, IList<Diagnostic> diagnostics)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _Root = root;
            Diagnostics = diagnostics == null || diagnostics.Count == 0 ? EmptyDiagnostics : diagnostics;
        }

        public IList<Diagnostic> Diagnostics { get; }
    }
}
