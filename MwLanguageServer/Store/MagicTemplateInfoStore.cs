using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Infrastructures;
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
        private readonly Trie<char, MagicTemplateInfo> caseSensitiveDict = new Trie<char, MagicTemplateInfo>();

        private readonly Trie<char, MagicTemplateInfo> caseInsensitiveDict =
            new Trie<char, MagicTemplateInfo>(CaseInsensitiveCharComparer.Default);

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
                    var dict = info.IsCaseSensitive ? caseSensitiveDict : caseInsensitiveDict;
                    dict.Add(info.Name, info);
                    if (info.Aliases != null)
                        foreach (var al in info.Aliases) dict.Add(al, info);
                }
            }
        }

        public IEnumerable<CompletionItem> GetTemplateCompletionItems(string prefix)
        {
            lock (caseSensitiveDict)
            {
                foreach (var p in caseSensitiveDict.WithPrefix(prefix))
                {
                    yield return new CompletionItem((string) p.Key, CompletionItemKind.Keyword,
                        p.Value.Summary, (string) p.Key);
                }
            }
            lock (caseInsensitiveDict)
            {
                foreach (var p in caseInsensitiveDict.WithPrefix(prefix))
                {
                    yield return new CompletionItem((string) p.Key, CompletionItemKind.Keyword,
                        p.Value.Summary, (string) p.Key);
                }
            }
        }
    }
}
