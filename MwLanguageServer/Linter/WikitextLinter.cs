using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Server;
using MwParserFromScratch;

namespace MwLanguageServer.Linter
{
    public class WikitextLinter
    {

        public WikitextLinter(WikitextParser parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            Parser = parser;
        }

        public WikitextParser Parser { get; }

        // This function should be thread-safe.
        public LintedWikitextDocument Lint(TextDocument document)
        {
            var ast = Parser.Parse(document.Content);
            var diag = new List<Diagnostic>();
            diag.Add(new Diagnostic(DiagnosticSeverity.Hint, new Range(0, 0, 0, 1), "TEST", "test message"));
            return new LintedWikitextDocument(ast, diag);
        }
    }
}