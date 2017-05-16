using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public WikitextParser Parser { get; }

        public LintedWikitextDocument Lint(TextDocument doc, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var emitter = new DiagnosticEmitter();
            //document = doc;
            try
            {
                var ast = Parser.Parse(doc.Content);
                ct.ThrowIfCancellationRequested();
                CheckMatchingPairs(ast, emitter);
                ct.ThrowIfCancellationRequested();
                CheckNodes(ast, emitter, ct);
                return new LintedWikitextDocument(doc, ast, emitter.Diagnostics);
            }
            finally
            {
                //document = null;
            }
        }

        private static readonly char[] newLineCharacters = {'\r', '\n'};

        private void CheckMatchingPairs(Node root, DiagnosticEmitter e)
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
                                e.OpenTagClosedByEndOfLine(boldSwitch.ToRange());
                            }
                            if (italicsSwitch != null && italicsSwitch != boldSwitch)
                            {
                                e.OpenTagClosedByEndOfLine(italicsSwitch.ToRange());
                                boldSwitch = null;
                            }
                            boldSwitch = null;
                            boldSwitch = italicsSwitch = null;
                        }
                        break;
                }
                CheckMatchingPairs(node, e);
            }
            if (boldSwitch != null)
            {
                e.OpenTagClosedByEndOfLine(boldSwitch.ToRange());
            }
            if (italicsSwitch != null && italicsSwitch != boldSwitch)
            {
                e.OpenTagClosedByEndOfLine(italicsSwitch.ToRange());
                boldSwitch = null;
            }
        }

        private void CheckNodes(Node root, DiagnosticEmitter e, CancellationToken ct)
        {
            foreach (var node in root.EnumDescendants())
            {
                ct.ThrowIfCancellationRequested();
                switch (node)
                {
                    case Template t:
                        CheckNode(t, e);
                        break;
                    case ArgumentReference ar:
                        CheckNode(ar, e);
                        break;
                    case TagNode tn:
                        CheckNode(tn, e);
                        break;
                    case WikiLink wl:
                        CheckNode(wl, e);
                        break;
                    case PlainText pt:
                        CheckNode(pt, e);
                        break;
                }
            }
        }

        private void CheckNode(Template template, DiagnosticEmitter e)
        {
            if (((IWikitextParsingInfo) template).InferredClosingMark)
                e.TransclusionNotClosed(template.ToRange(), MwParserUtility.NormalizeTitle(template.Name));
            if (string.IsNullOrWhiteSpace(template.Name?.ToString()))
                e.EmptyTransclusionTarget(template.ToRange());
            var names = new HashSet<string>();
            foreach (var p in template.Arguments.EnumNameArgumentPairs())
            {
                if (!names.Add(p.Key))
                    e.DuplicateTemplateArgument(p.Value.ToRange(), p.Key, MwParserUtility.NormalizeTitle(template.Name));
            }
        }

        private void CheckNode(ArgumentReference ar, DiagnosticEmitter e)
        {
            if (((IWikitextParsingInfo)ar).InferredClosingMark)
                e.TransclusionNotClosed(ar.ToRange(), ar.Name?.ToString());
        }

        private void CheckNode(TagNode tn, DiagnosticEmitter e)
        {
            if (((IWikitextParsingInfo)tn).InferredClosingMark)
                e.OpenTagNotClosed(tn.ToRange(), tn.Name);
            var names = new HashSet<string>();
            foreach (var attr in tn.Attributes)
            {
                if (attr.Name == null) continue;
                var name = attr.Name.ToString().Trim();
                if (!names.Add(name))
                    e.DuplicateTagAttribute(attr.ToRange(), name, tn.Name);
            }
        }

        private void CheckNode(WikiLink link, DiagnosticEmitter e)
        {
            if (string.IsNullOrWhiteSpace(link.Target?.ToString()))
                e.EmptyWikilinkTarget(link.ToRange());
        }

        private static readonly Regex magicLinkMatcher =
            new Regex(@"\b((RFC|PMID)\s+\d+|ISBN\s+(97[89]-?)?(\d-?){9}[\dX])\b", RegexOptions.IgnoreCase);

        private void CheckNode(PlainText pt, DiagnosticEmitter e)
        {
            if (string.IsNullOrEmpty(pt.Content)) return;
            if (magicLinkMatcher.IsMatch(pt.Content))
                e.HardCodedMagicLink(pt.ToRange());
        }
    }
}