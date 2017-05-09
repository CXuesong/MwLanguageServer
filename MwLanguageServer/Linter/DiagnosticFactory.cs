using System;
using System.Collections.Generic;
using System.Text;
using LanguageServer.VsCode.Contracts;

namespace MwLanguageServer.Linter
{
    public class DiagnosticFactory
    {
        public const string SourceName = "Wikitext";

        public Diagnostic OpenTagClosedByEndOfLine(Range range)
        {
            return new Diagnostic(DiagnosticSeverity.Warning, range, SourceName,
                "Open tag is implicitly closed by end of line.");
        }
    }
}
