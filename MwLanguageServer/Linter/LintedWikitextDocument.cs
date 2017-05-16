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

        public LintedWikitextDocument(TextDocument textDocument, Wikitext root, ICollection<Diagnostic> diagnostics)
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

        public ICollection<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Inferss linked/transcluded pages information, and stores it into global store.
        /// </summary>
        public int InferTemplateInformation(PageInfoStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            int ct = 0;
            // template, argument
            var dict = new Dictionary<string, Dictionary<string, TemplateArgumentInfo>>();
            foreach (var template in _Root.EnumDescendants().OfType<Template>())
            {
                if (template.IsMagicWord) continue;
                var name = template.Name?.ToString();
                if (string.IsNullOrEmpty(name)) continue;
                name = MwParserUtility.NormalizeTitle(name);
                if (name.Contains('{') || name.Contains('}')) continue;
                name = Utility.ExpandTransclusionTitle(name);
                // Start to infer it.
                if (!dict.TryGetValue(name, out var parameters))
                {
                    if (store.ContainsPageInfo(name)) continue;
                    parameters = new Dictionary<string, TemplateArgumentInfo>();
                    dict.Add(name, parameters);
                }
                foreach (var p in template.Arguments.EnumNameArgumentPairs())
                {
                    if (parameters.ContainsKey(p.Key)) continue;
                    // TODO: Insert documentation here.
                    parameters.Add(p.Key, new TemplateArgumentInfo(p.Key, null));
                }
            }
            foreach (var p in dict)
            {
                var isTemplate = Utility.IsTemplateTitle(p.Key);
                string transclusionName;
                if (isTemplate)
                {
                    Debug.Assert(p.Key.StartsWith("Template:"));
                    transclusionName = p.Key.Substring(9);
                }
                else
                {
                    transclusionName = ":" + p.Key;
                }
                if (store.UpdatePageInfo(new PageInfo(p.Key, transclusionName, Prompts.InferredPageInfo,
                    p.Value.OrderBy(p1 => p1.Key, TemplateArgumentNameComparer.Default)
                        .Select(p1 => p1.Value).ToArray(),
                    isTemplate, true))) ct++;
            }
            return ct;
        }

        public Hover GetHover(Position position)
        {
            var node = TraceNode(position);
            if (node == null) return null;
            Node prevNode = null;
            var nodeTrace = new List<string>();
            IWikitextLineInfo focusNode = null;
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
            Debug.Assert(focusNode.HasLineInfo);
            nodeTrace.Reverse();
            return new Hover(string.Join(" → ", nodeTrace), focusNode.ToRange());
        }

        public SignatureHelp GetSignatureHelp(Position position, MagicTemplateInfoStore magicStore, PageInfoStore store)
        {
            var node = TraceNode(position);
            Node lastNode = null;
            while (node != null)
            {
                switch (node)
                {
                    case Template template:
                        if (template.IsMagicWord)
                        {
                            var info = magicStore.TryGetInfo(template.Name?.ToString().Trim());
                            // E.g. non-existent, sharp(#)-leading template names
                            if (info == null) return null;
                            var help = new SignatureHelp
                            {
                                Signatures = info.ToSignatureInformation(),
                                ActiveSignature = 0,
                                ActiveParameter = -1,
                            };
                            // Magic Words are always positional, while it can fake "named arguments"
                            if (lastNode is TemplateArgument arg)
                            {
                                var argIndex = template.Arguments.IndexOf(arg);
                                help.ActiveSignature = info.Signatures.IndexOf(s => s.Count > argIndex);
                                help.ActiveParameter = argIndex;
                            }
                            return help;
                        }
                        else
                        {
                            var templateName = MwParserUtility.NormalizeTitle(template.Name);
                            if (string.IsNullOrEmpty(templateName)) return null;
                            templateName = Utility.ExpandTransclusionTitle(templateName);
                            var templateInfo = store.TryGetPageInfo(templateName);
                            if (templateInfo == null) return null;
                            var help = new SignatureHelp
                            {
                                Signatures = new[] {templateInfo.ToSignatureInformation()},
                                ActiveSignature = 0,
                                ActiveParameter = -1,
                            };
                            if (lastNode is TemplateArgument arg)
                            {
                                var argName = arg.ArgumentName();
                                help.ActiveParameter = templateInfo.Arguments.IndexOf(p => p.Name == argName);
                            }
                            return help;
                        }
                    case WikiLink wikiLink:
                        return null;
                }
                lastNode = node;
                node = node.ParentNode;
            }
            return null;
        }

        /// <summary>
        /// Gets the text to the left-hand-side of the cart. Used for auto-completion.
        /// </summary>
        private string GetTypedLhsText(InlineContainer node, Position caretPosition)
        {
            var firstPt = node?.Inlines.FirstNode as PlainText;
            if (firstPt == null) return null;
            IWikitextLineInfo li = node;
            Debug.Assert(li.HasLineInfo);
            var startPos = new Position(li.StartLineNumber, li.StartLinePosition);
            Debug.Assert(startPos <= caretPosition);
            Debug.Assert(new Position(li.EndLineNumber, li.EndLinePosition) >= caretPosition);
            return TextDocument.GetRange(new Range(startPos, caretPosition));
        }

        public IEnumerable<CompletionItem> GetCompletionItems(Position position, MagicTemplateInfoStore magicStore, PageInfoStore store)
        {
            var node = TraceNode(position);
            Node lastNode = null;
            while (node != null)
            {
                switch (node)
                {
                    case Template template:
                        if (lastNode != null && lastNode == template.Name)
                        {
                            var enteredName = GetTypedLhsText(template.Name, position);
                            return magicStore.GetTemplateCompletionItems(enteredName)
                                .Concat(store.GetTemplateCompletionItems());
                        }
                        return null;
                    case WikiLink wikiLink:
                        if (lastNode != null && lastNode == wikiLink.Target)
                        {
                            var enteredName = GetTypedLhsText(wikiLink.Target, position);
                            return store.GetWikiLinkCompletionItems();
                        }
                        return null;
                }
                lastNode = node;
                node = node.ParentNode;
            }
            return null;
        }

        private Node TraceNode(Position position)
        {
            return TraceNode(_Root, position);
        }

        private Node TraceNode(Node root, Position position)
        {
            foreach (var node in root.EnumChildren())
            {
                IWikitextLineInfo span = node;
                Debug.Assert(span.HasLineInfo);
                if (span.StartLineNumber > position.Line) continue;
                if (span.EndLineNumber < position.Line) continue;
                if (span.StartLineNumber == position.Line && span.StartLinePosition > position.Character) continue;
                if (span.EndLineNumber == position.Line && span.EndLinePosition < position.Character) continue;
                return TraceNode(node, position);
            }
            return root;
        }
    }
}
