using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public LintedWikitextDocument Lint(TextDocument doc, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            document = doc;
            try
            {
                var ast = Parser.Parse(doc.Content);
                ct.ThrowIfCancellationRequested();
                var diag = new List<Diagnostic>();
                diag.AddRange(CheckMatchingPairs(ast));
                ct.ThrowIfCancellationRequested();
                diag.AddRange(CheckDuplicateArguments(ast));
                return new LintedWikitextDocument(doc, ast, diag);
            }
            finally
            {
                document = null;
            }
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

        private IEnumerable<Diagnostic> CheckDuplicateArguments(Node root)
        {
            foreach (var node in root.EnumDescendants())
            {
                if (node is Template tp)
                {
                    var names = new HashSet<string>();
                    foreach (var p in tp.Arguments.EnumNameArgumentPairs())
                    {
                        if (!names.Add(p.Key))
                            yield return df.DuplicateTemplateArgument(RangeOf(p.Value), p.Key, MwParserUtility.NormalizeTitle(tp.Name));
                    }
                } else if (node is TagNode tag)
                {
                    var names = new HashSet<string>();
                    foreach (var attr in tag.Attributes)
                    {
                        if (attr.Name == null) continue;
                        var name = attr.Name.ToString().Trim();
                        if (!names.Add(name))
                            yield return df.DuplicateTagAttribute(RangeOf(attr), name, tag.Name);
                    }
                }
            }
        }
    }
}