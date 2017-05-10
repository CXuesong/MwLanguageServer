using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Server;
using MwLanguageServer.Localizable;
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
            templateParametersDict = new Lazy<IDictionary<string, IList<ParameterInformation>>>(BuildTemplateSignatureDict);
        }

        // We need to assume the content of _Root is readonly.
        private readonly Wikitext _Root;

        private readonly Lazy<IDictionary<string, IList<ParameterInformation>>> templateParametersDict;

        public TextDocument TextDocument { get; }

        public IList<Diagnostic> Diagnostics { get; }

        private IDictionary<string, IList<ParameterInformation>> BuildTemplateSignatureDict()
        {
            // template, argument
            var argumentSet = new HashSet<(string, string)>();
            var dict = new Dictionary<string, IList<ParameterInformation>>();
            foreach (var template in _Root.EnumDescendants().OfType<Template>())
            {
                var name = MwParserUtility.NormalizeTitle(template.Name);
                if (string.IsNullOrEmpty(name)) continue;
                if (!dict.TryGetValue(name, out var parameters))
                {
                    parameters = new List<ParameterInformation>();
                    dict.Add(name, parameters);
                }
                foreach (var p in template.Arguments.EnumNameArgumentPairs())
                {
                    if (argumentSet.Contains((name, p.Key))) continue;
                    argumentSet.Add((name, p.Key));
                    // TODO: Insert documentation here.
                    parameters.Add(new ParameterInformation(p.Key, Utility.EscapeMd(p.Key)));
                }
            }
            return dict;
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

        public SignatureHelp GetSignatureHelp(Position position)
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
            if (templateParametersDict.Value.TryGetValue(templateName, out var pa))
            {
                var help = new SignatureHelp
                {
                    Signatures =
                        new List<SignatureInformation>
                        {
                            new SignatureInformation(Utility.NodeToMd(template),
                                template.IsMagicWord ? Prompts.TemplateMagicNode : Prompts.TemplateNode,
                                pa)
                        },
                    ActiveSignature = 0
                };
                if (lastNode is TemplateArgument arg)
                {
                    var argName = arg.ArgumentName();
                    help.ActiveParameter = pa.IndexOf(p => p.Label == argName);
                }
                return help;
            }
            return null;
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
