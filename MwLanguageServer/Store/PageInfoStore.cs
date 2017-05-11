using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using MwLanguageServer.Linter;

namespace MwLanguageServer.Store
{
    public class PageInfoStore
    {
        private readonly ConcurrentDictionary<string, PageInfo> pageInfoDict = new ConcurrentDictionary<string, PageInfo>();

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
            var newInfo = pageInfoDict.AddOrUpdate(info.Name, info, (k, old) =>
            {
                if (!old.IsInferred && info.IsInferred) return old;
                return info;
            });
            return newInfo == info;
        }
    }
}
