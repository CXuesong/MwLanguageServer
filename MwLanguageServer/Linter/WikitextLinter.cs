using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Server;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace MwLanguageServer.Linter
{
    // Not thread-safe
    public class WikitextLinter
    {

        public WikitextLinter(WikitextParser parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            Parser = parser;
        }

        private static readonly DiagnosticFactory df = new DiagnosticFactory();

        private TextDocument document;

        private Range RangeOf(IWikitextSpanInfo thisNode)
        {
            Debug.Assert(thisNode.HasSpanInfo);
            return new Range(document.PositionAt(thisNode.Start),
                document.PositionAt(thisNode.Start + thisNode.Length));
        }

        public WikitextParser Parser { get; }

        public LintedWikitextDocument Lint(TextDocument doc)
        {
            document = doc;
            var ast = Parser.Parse(doc.Content);
            var diag = new List<Diagnostic>();
            diag.AddRange(CheckMatchingPairs(ast));
            return new LintedWikitextDocument(ast, diag);
        }

        private static readonly char[] newLineCharacters = {'\r', '\n'};

        private IEnumerable<Diagnostic> CheckMatchingPairs(Node root)
        {
            FormatSwitch boldSwitch = null, italicsSwitch = null;
            foreach (var node in root.EnumChildren())
            {
                switch (node)
                {
                    case FormatSwitch fs:
                        if (fs.SwitchBold) boldSwitch = boldSwitch == null ? fs : null;
                        if (fs.SwitchItalics) italicsSwitch = italicsSwitch == null ? fs : null;
                        break;
                    case PlainText pt:
                        if (pt.Content.IndexOfAny(newLineCharacters) >= 0)
                        {
                            // a line-break will reset either bold or itablics
                            if (boldSwitch != null)
                            {
                                yield return df.OpenTagClosedByEndOfLine(RangeOf(boldSwitch));
                            }
                            if (italicsSwitch != null && italicsSwitch != boldSwitch)
                            {
                                yield return df.OpenTagClosedByEndOfLine(RangeOf(italicsSwitch));
                                boldSwitch = null;
                            }
                            boldSwitch = null;
                            boldSwitch = italicsSwitch = null;
                        }
                        break;
                }
                foreach (var diag in CheckMatchingPairs(node)) yield return diag;
            }
            if (boldSwitch != null)
            {
                yield return df.OpenTagClosedByEndOfLine(RangeOf(boldSwitch));
            }
            if (italicsSwitch != null && italicsSwitch != boldSwitch)
            {
                yield return df.OpenTagClosedByEndOfLine(RangeOf(italicsSwitch));
                boldSwitch = null;
            }
        }
    }
}