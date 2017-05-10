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
                    TraceNode(node, offset, trace);
                    trace.Nodes.Add(node);
                    return;
                }
            }
        }

        private class AstTrace
        {
            // From innermost to outermost
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
                        case Template t:
                        case TemplateArgument _:
                        case ArgumentReference _:
                        case WikiLink _:
                        case ExternalLink _:
                        case FormatSwitch _:
                            if (focusNode == null) focusNode = Nodes[i];
                            contentBuilder.AppendLine(Utility.NodeToMd(Nodes[i]));
                            contentBuilder.AppendLine();
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
