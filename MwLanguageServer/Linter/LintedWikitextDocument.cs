using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Server;
using MwParserFromScratch;
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

        public Hover GetHover(Position position, TextDocument doc)
        {
            return GetHover(doc.OffsetAt(position), doc);
        }

        public Hover GetHover(int offset, TextDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            var trace = TraceNode(offset);
            return trace.BuildHover(doc);
        }

        private AstTrace TraceNode(int offset)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            var trace = new AstTrace();
            TraceNode(_Root, offset, trace);
            return trace;
        }

        private void TraceNode(Node root, int offset, AstTrace trace)
        {
            foreach (var node in root.EnumChildren())
            {
                IWikitextSpanInfo span = node;
                Debug.Assert(span.HasSpanInfo);
                if (offset > span.Start && offset < span.Start + span.Length)
                {
                    trace.Nodes.Add(node);
                    TraceNode(node, offset, trace);
                    return;
                }
            }
        }

        private class AstTrace
        {
            // From outermost to innermost
            public IList<Node> Nodes { get; } = new List<Node>();

            public Hover BuildHover(TextDocument doc)
            {
                if (Nodes.Count == 0) return null;
                var contentBuilder = new StringBuilder();
                IWikitextSpanInfo focusNode = null;
                for (int i = 0; i < Nodes.Count; i++)
                {

                    switch (Nodes[i])
                    {
                        case Template _:
                            if (i + 1 < Nodes.Count && Nodes[i + 1] is TemplateArgument)
                                continue; // We will show template name in NodeToMd(TemplateArgument)
                            goto SHOW_NODE;
                        case TagNode _:
                            if (i + 1 < Nodes.Count && Nodes[i + 1] is TagNode)
                                continue; // We will show template name in NodeToMd(TagAttribute)
                            goto SHOW_NODE;
                        case TemplateArgument _:
                        case ArgumentReference _:
                        case WikiLink _:
                        case ExternalLink _:
                        case FormatSwitch _:
                        case TagAttribute _:
                        case Comment _:
                            SHOW_NODE:
                            focusNode = Nodes[i];
                            if (contentBuilder.Length > 0) contentBuilder.Append(" → ");
                            contentBuilder.Append(Utility.NodeToMd(Nodes[i]));
                            break;
                    }
                }
                if (focusNode == null) return null;
                Debug.Assert(focusNode.HasSpanInfo);
                return new Hover(contentBuilder.ToString(),
                    new Range(doc.PositionAt(focusNode.Start),
                        doc.PositionAt(focusNode.Start + focusNode.Length)));
            }
        }
    }
}
