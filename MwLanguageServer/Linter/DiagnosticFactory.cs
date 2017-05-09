using System;
using System.Collections.Generic;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Localizable;

namespace MwLanguageServer.Linter
{
    public class DiagnosticFactory
    {
        public const string SourceName = "Wikitext";

        public Diagnostic OpenTagClosedByEndOfLine(Range range)
        {
            return new Diagnostic(DiagnosticSeverity.Warning, range, SourceName, Diagnostics.OpenTagClosedByEndOfLine);
        }
    }
}
