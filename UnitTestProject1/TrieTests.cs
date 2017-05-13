using System;
using System.Collections.Generic;
using System.Linq;
using MwLanguageServer.Infrastructures;
using Xunit;

namespace UnitTestProject1
{
    public class TrieTests
    {
        [Fact]
        public void BasicTest()
        {
            var helper = new TestHelper<int>();
            helper.Add("test", 1);
            helper.Add("tesT", 2);
            helper.Add("tesa", 3);
            helper.Add("tesb", 4);
            helper.Add("tesabc", 5);
            helper.Add("tesab", 6);
            helper.Add("tew", 7);
            helper.Add("tea", 8);
            helper.Add("b", 10);
            helper.TestIndexer();
            helper.TestIndexer("a");
            helper.TestIndexer("tes");
            helper.TestEnumerator();
            helper.TestKeyEnumerator();
            helper.TestValueEnumerator();
            helper.TestPrefix("t");
            helper.TestPrefix("te");
            helper.TestPrefix("tes");
            helper.TestPrefix("test");
            helper.TestPrefix("x");
            helper.Remove("a");
            helper.Remove("tes");
            helper.Remove("tesT");
            helper.TestIndexer();
            helper.TestIndexer("tesT");
            helper.Remove("tesT");
            helper.TestEnumerator();
            helper.TestKeyEnumerator();
            helper.TestValueEnumerator();
        }

        public class TestHelper<TValue>
        {
            private readonly Dictionary<string, TValue> dict = new Dictionary<string, TValue>();
            private readonly Trie<char, TValue> trie = new Trie<char, TValue>();

            public TestHelper()
            {
                Assert.Equal(0, trie.Count);
            }

            public bool Add(string key, TValue value)
            {
                if (dict.ContainsKey(key))
                {
                    Assert.Throws<InvalidOperationException>(() => trie.Add(key, value));
                    Assert.Equal(dict.Count, trie.Count);
                    return false;
                }
                else
                {
                    dict.Add(key, value);
                    trie.Add(key, value);
                    Assert.Equal(dict.Count, trie.Count);
                    Assert.True(trie.TryGetValue(key, out var v));
                    Assert.Equal(value, v);
                    return true;
                }
            }

            public void Remove(string key)
            {
                var result = dict.Remove(key);
                Assert.Equal(result, trie.Remove(key));
                Assert.Equal(dict.Count, trie.Count);
                Assert.False(trie.TryGetValue(key, out _));
            }

            public void Clear(string key)
            {
                dict.Clear();
                trie.Clear();
                Assert.Equal(0, trie.Count);
            }

            public void TestIndexer()
            {
                foreach (var p in dict)
                {
                    Assert.Equal(p.Value, trie[p.Key]);
                }
            }

            public void TestIndexer(string key)
            {
                if (dict.ContainsKey(key)) Assert.Equal(dict[key], trie[key]);
                else Assert.Throws<KeyNotFoundException>(() => trie[key]);
            }

            public void TestEnumerator()
            {
                var dictPairs = new HashSet<KeyValuePair<string, TValue>>(dict);
                var triePairs = new HashSet<KeyValuePair<string, TValue>>(trie.Select(p =>
                    new KeyValuePair<string, TValue>((string) p.Key, p.Value)));
                Assert.Equal(dictPairs, triePairs);
            }

            public void TestKeyEnumerator()
            {
                var dictPairs = new HashSet<string>(dict.Keys);
                var triePairs = new HashSet<string>(trie.Keys.Select(s => (string) s));
                Assert.Equal(dictPairs, triePairs);
            }

            public void TestValueEnumerator()
            {
                var dictPairs = new HashSet<TValue>(dict.Values);
                var triePairs = new HashSet<TValue>(trie.Values);
                Assert.Equal(dictPairs, triePairs);
            }

            public void TestPrefix(string prefix)
            {
                var dictPairs = new HashSet<KeyValuePair<string, TValue>>(dict.Where(p => p.Key.StartsWith(prefix)));
                var triePairs = new HashSet<KeyValuePair<string, TValue>>(trie.WithPrefix(prefix).Select(p =>
                    new KeyValuePair<string, TValue>((string) p.Key, p.Value)));
                Assert.Equal(dictPairs, triePairs);
            }
        }

    }
}
