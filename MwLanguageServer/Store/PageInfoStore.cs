using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Infrastructures;
using MwLanguageServer.Linter;
using MwLanguageServer.Localizable;
using MwLanguageServer.Store.Contracts;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MwLanguageServer.Store
{
    public class PageInfoStore
    {

        private readonly Trie<char, PageInfo> linkDict = new Trie<char, PageInfo>(CaseInsensitiveCharComparer.Default);
        private readonly Trie<char, PageInfo> transclusionDict = new Trie<char, PageInfo>(CaseInsensitiveCharComparer.Default);
        // private readonly ConcurrentDictionary<PageInfo, SignatureInformation> signatureInformationCahce = new ConcurrentDictionary<PageInfo, SignatureInformation>();

        public PageInfo TryGetPageInfo(string normalizedTitle)
        {
            lock (linkDict)
                if (linkDict.TryGetValue(normalizedTitle, out var pi)) return pi;
            return null;
        }

        public PageInfo TryGetTransclusionPageInfo(string transclusionTitle)
        {
            lock (linkDict)
                if (transclusionDict.TryGetValue(transclusionTitle, out var pi)) return pi;
            return null;
        }

        /// <returns><c>null</c> if there's no redirect.</returns>
        public PageInfo ResolveRedirectTarget(PageInfo pageInfo)
        {
            if (pageInfo == null) throw new ArgumentNullException(nameof(pageInfo));
            var cur = pageInfo;
            if (string.IsNullOrEmpty(cur.RedirectTarget)) return null;
            NEXT:
            var next = cur.Type == PageType.MagicWord ? TryGetTransclusionPageInfo(cur.RedirectTarget) : TryGetPageInfo(cur.RedirectTarget);
            if (next == null) return cur;
            cur = next;
            if (string.IsNullOrEmpty(cur.RedirectTarget)) return cur;
            goto NEXT;
        }

        public bool ContainsPageInfo(string normalizedTitle)
        {
            return linkDict.ContainsKey(normalizedTitle);
        }

        public void UpdatePageInfo(PageInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (info.Type != PageType.MagicWord)
            {
                lock (linkDict) linkDict[info.FullName] = info;
            }
            lock (transclusionDict) transclusionDict[info.TransclusionName] = info;
        }

        public IReadOnlyCollection<CompletionItem> GetWikiLinkCompletionItems(string prefix)
        {
            var lwp = GetLastWordingPosition(prefix);
            lock (linkDict)
            {
                return linkDict.WithPrefix(prefix).Select(p => new CompletionItem(
                        p.Value.FullName.Substring(lwp), MapCompletionItemKind(p.Value.Type), p.Value.Summary,
                        p.Value.FullName.Substring(lwp))).ToImmutableArray();
            }
        }

        public IReadOnlyCollection<CompletionItem> GetTransclusionCompletionItems(string prefix)
        {
            var lwp = GetLastWordingPosition(prefix);
            lock (transclusionDict)
            {
                return transclusionDict.WithPrefix(prefix).Select(p => new CompletionItem(
                        p.Value.TransclusionName.Substring(lwp), MapCompletionItemKind(p.Value.Type), p.Value.Summary,
                        p.Value.TransclusionName.Substring(lwp))).ToImmutableArray();
            }
        }

        private static readonly Regex lastWordingPrefixMatcher = new Regex(@"\w+$", RegexOptions.RightToLeft);

        private static int GetLastWordingPosition(string word)
        {
            // Issue#1 Find the rightmost starting position of non-symbol characters.
            // E.g.   #switch
            //         ^ Find this index
            //        test-page-name
            //                  ^ Find this index
            var lastWordingPrefix = lastWordingPrefixMatcher.Match(word);
            return lastWordingPrefix.Success ? lastWordingPrefix.Index : word.Length;
        }

        private static CompletionItemKind MapCompletionItemKind(PageType pageType)
        {
            switch (pageType)
            {
                case PageType.Page:
                    return CompletionItemKind.Unit;
                case PageType.MagicWord:
                    return CompletionItemKind.Keyword;
                case PageType.Template:
                    return CompletionItemKind.Function;
                default:
                    return CompletionItemKind.Value;
            }
        }

        public void LoadLocalStore()
        {
            JArray templates;
            using (var reader = new JsonTextReader(LocalizableUtility.OpenTextReader(ContentIndex.MagicTemplates)))
                templates = JArray.Load(reader);
            lock (transclusionDict)
                foreach (var template in templates)
                {
                    //var dict = info.IsCaseSensitive ? caseSensitiveDict : caseInsensitiveDict;
                    var name = (string)template["Name"];
                    transclusionDict.Add(name, new PageInfo(name, name, (string) template["RedirectTarget"],
                        (string) template["Summary"],
                        template["Arguments"]
                            ?.Select(t => new TemplateArgumentInfo((string) t["Name"], (string) t["Summary"]))
                            .ToImmutableArray(),
                        PageType.MagicWord, false));
                }
        }

        public IList<PageInfo> Dump()
        {
            return transclusionDict.Values.ToList();
        }
    }
}
