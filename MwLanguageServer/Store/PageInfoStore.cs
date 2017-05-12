using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Linter;

namespace MwLanguageServer.Store
{
    public class PageInfoStore
    {
        private readonly ConcurrentDictionary<string, PageInfo> pageInfoDict =
            new ConcurrentDictionary<string, PageInfo>();

        private readonly object wikiLinkCompletionItems_lock = new object();
        private readonly object templateCompletionItems_lock = new object();

        private IReadOnlyCollection<CompletionItem> wikiLinkCompletionItems;
        private IReadOnlyCollection<CompletionItem> templateCompletionItems;

        public PageInfo TryGetPageInfo(string normalizedTitle)
        {
            if (pageInfoDict.TryGetValue(normalizedTitle, out var pi)) return pi;
            return null;
        }

        public bool ContainsPageInfo(string normalizedTitle)
        {
            return pageInfoDict.ContainsKey(normalizedTitle);
        }

        public bool UpdatePageInfo(PageInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            var newInfo = pageInfoDict.AddOrUpdate(info.FullName, info, (k, old) =>
            {
                if (!old.IsInferred && info.IsInferred) return old;
                return info;
            });
            if (newInfo == info)
            {
                lock (wikiLinkCompletionItems_lock) wikiLinkCompletionItems = null;
                lock (templateCompletionItems_lock) templateCompletionItems = null;
                return true;
            }
            return false;
        }

        public IReadOnlyCollection<CompletionItem> GetWikiLinkCompletionItems()
        {
            lock (wikiLinkCompletionItems_lock)
            {
                if (wikiLinkCompletionItems == null)
                {
                    wikiLinkCompletionItems = pageInfoDict.Values.Select(pi => new CompletionItem(
                        pi.FullName, pi.IsTemplate ? CompletionItemKind.Function : CompletionItemKind.File, pi.Summary,
                        pi.FullName)).ToImmutableArray();
                }
                return wikiLinkCompletionItems;
            }
        }

        public IReadOnlyCollection<CompletionItem> GetTemplateCompletionItems()
        {
            lock (templateCompletionItems_lock)
            {
                if (templateCompletionItems == null)
                {
                    templateCompletionItems = pageInfoDict.Values.Select(pi => new CompletionItem(
                        pi.TransclusionName, pi.IsTemplate ? CompletionItemKind.Function : CompletionItemKind.File,
                        pi.Summary, pi.TransclusionName)).ToImmutableArray();
                }
                return templateCompletionItems;
            }
        }
    }
}
