using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Localizable;
using Newtonsoft.Json;

namespace MwLanguageServer.Store
{
    public class MagicTemplateInfoStore
    {

        public MagicTemplateInfoStore()
        {
        }

        private static readonly JsonSerializer serializer = new JsonSerializer();

        private readonly Dictionary<string, MagicTemplateInfo> caseSensitiveDict = new Dictionary<string, MagicTemplateInfo>();
        private readonly Dictionary<string, MagicTemplateInfo> caseInsensitiveDict = new Dictionary<string, MagicTemplateInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly object templateCompletionItems_lock = new object();
        private IReadOnlyCollection<CompletionItem> templateCompletionItems;

        public MagicTemplateInfo TryGetInfo(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            lock (caseSensitiveDict)
                if (caseSensitiveDict.TryGetValue(name, out var info)) return info;
            lock (caseInsensitiveDict)
                if (caseInsensitiveDict.TryGetValue(name, out var info)) return info;
            return null;
        }

        public void LoadLocalStore()
        {
            ICollection<MagicTemplateInfo> infoCollection;
            using (var reader = new JsonTextReader(LocalizableUtility.OpenTextReader(ContentIndex.MagicTemplates)))
                infoCollection = serializer.Deserialize<ICollection<MagicTemplateInfo>>(reader);
            lock (caseSensitiveDict)
            lock (caseInsensitiveDict)
            {
                foreach (var info in infoCollection)
                {
                    IDictionary<string, MagicTemplateInfo> dict =
                        info.IsCaseSensitive ? caseSensitiveDict : caseInsensitiveDict;
                    dict.Add(info.Name, info);
                    if (info.Aliases != null)
                        foreach (var al in info.Aliases) dict.Add(al, info);
                }
            }
        }

        public IReadOnlyCollection<CompletionItem> GetTemplateCompletionItems()
        {
            lock (templateCompletionItems_lock)
            {
                if (templateCompletionItems == null)
                {
                    var builder = ImmutableArray.CreateBuilder<CompletionItem>();
                    lock (caseSensitiveDict)
                    {
                        builder.Capacity = caseSensitiveDict.Count;
                        foreach (var p in caseSensitiveDict)
                        {
                            builder.Add(new CompletionItem(p.Key, CompletionItemKind.Keyword, p.Value.Summary, p.Key));
                        }
                    }
                    lock (caseInsensitiveDict)
                    {
                        builder.Capacity += caseInsensitiveDict.Count;
                        foreach (var p in caseInsensitiveDict)
                        {
                            builder.Add(new CompletionItem(p.Key, CompletionItemKind.Keyword, p.Value.Summary, p.Key));
                        }
                    }
                    templateCompletionItems = builder.MoveToImmutable();
                }
                return templateCompletionItems;
            }
        }

    }
}
