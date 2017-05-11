using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Server;
using MwLanguageServer.Localizable;
using MwLanguageServer.Store;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace MwLanguageServer.Linter
{
    public class LintedWikitextDocument
    {

        public LintedWikitextDocument(TextDocument textDocument, Wikitext root, IList<Diagnostic> diagnostics)
        {
            if (textDocument == null) throw new ArgumentNullException(nameof(textDocument));
            if (root == null) throw new ArgumentNullException(nameof(root));
            TextDocument = textDocument;
            _Root = root;
            Diagnostics = diagnostics == null || diagnostics.Count == 0 ? Diagnostic.EmptyDiagnostics : diagnostics;
        }

        // We need to assume the content of _Root is readonly.
        private readonly Wikitext _Root;

        public TextDocument TextDocument { get; }

        public IList<Diagnostic> Diagnostics { get; }

        public int InferTemplateInformation(PageInfoStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            int ct = 0;
            // template, argument
            var argumentSet = new HashSet<(string, string)>();
            var dict = new Dictionary<string, List<TemplateArgumentInfo>>();
            foreach (var template in _Root.EnumDescendants().OfType<Template>())
            {
                var name = MwParserUtility.NormalizeTitle(template.Name);
                if (string.IsNullOrEmpty(name)) continue;
                name = Utility.ExpandTransclusionTitle(name);
                // Start to infer it.
                if (!dict.TryGetValue(name, out var parameters))
                {
                    if (store.ContainsPageInfo(name)) continue;
                    parameters = new List<TemplateArgumentInfo>();
                    dict.Add(name, parameters);
                }
                foreach (var p in template.Arguments.EnumNameArgumentPairs())
                {
                    if (argumentSet.Contains((name, p.Key))) continue;
                    argumentSet.Add((name, p.Key));
                    // TODO: Insert documentation here.
                    parameters.Add(new TemplateArgumentInfo(p.Key, null));
                }
            }
            foreach (var p in dict)
            {
                if (store.UpdatePageInfo(new PageInfo(p.Key, Prompts.InferredPageInfo, p.Value, true))) ct++;
            }
            return ct;
        }

        public Hover GetHover(Position position)
        {
            var node = TraceNode(position);
            if (node == null) return null;
            Node prevNode = null;
            var nodeTrace = new List<string>();
            IWikitextSpanInfo focusNode = null;
            while (node != null)
            {
                switch (node)
                {
                    case Template _:
                        if (prevNode is TemplateArgument)
                            break;      // We will show template name in NodeToMd(TemplateArgument)
                        goto SHOW_NODE;
                    case TagNode _:
                        if (prevNode is TagAttribute)
                            break;      // We will show template name in NodeToMd(TagAttribute)
                        goto SHOW_NODE;
                    case TemplateArgument _:
                    case ArgumentReference _:
                    case WikiLink _:
                    case ExternalLink _:
                    case FormatSwitch _:
                    case TagAttribute _:
                    case Comment _:
                        SHOW_NODE:
                        if (focusNode == null) focusNode = node;
                        nodeTrace.Add(Utility.NodeToMd(node));
                        break;
                }
                prevNode = node;
                node = node.ParentNode;
            }
            if (focusNode == null) return null;
            Debug.Assert(focusNode.HasSpanInfo);
            nodeTrace.Reverse();
            return new Hover(string.Join(" → ", nodeTrace), new Range(
                TextDocument.PositionAt(focusNode.Start),
                TextDocument.PositionAt(focusNode.Start + focusNode.Length)));
        }

        public SignatureHelp GetSignatureHelp(Position position, PageInfoStore store)
        {
            var node = TraceNode(position);
            Node lastNode = null;
            while (node != null)
            {
                if (node is Template) break;
                lastNode = node;
                node = node.ParentNode;
            }
            if (node == null) return null;
            var template = (Template) node;
            var templateName = MwParserUtility.NormalizeTitle(template.Name);
            if (string.IsNullOrEmpty(templateName)) return null;
            templateName = Utility.ExpandTransclusionTitle(templateName);
            var templateInfo = store.TryGetPageInfo(templateName);
            if (templateInfo == null) return null;
            var help = new SignatureHelp
            {
                Signatures = new[] {templateInfo.ToSignatureInformation()},
                ActiveSignature = 0
            };
            if (lastNode is TemplateArgument arg)
            {
                var argName = arg.ArgumentName();
                help.ActiveParameter = templateInfo.Arguments.IndexOf(p => p.Name == argName);
            }
            return help;
        }

        private Node TraceNode(Position position)
        {
            return TraceNode(TextDocument.OffsetAt(position));
        }

        private Node TraceNode(int offset)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            return TraceNode(_Root, offset);
        }

        private Node TraceNode(Node root, int offset)
        {
            foreach (var node in root.EnumChildren())
            {
                IWikitextSpanInfo span = node;
                Debug.Assert(span.HasSpanInfo);
                if (offset > span.Start && offset < span.Start + span.Length)
                    return TraceNode(node, offset);
            }
            return root;
        }
    }
}
