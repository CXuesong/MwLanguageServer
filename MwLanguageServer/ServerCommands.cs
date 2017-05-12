using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MwLanguageServer
{
    /// <summary>
    /// MwLanguageServer-provided command names.
    /// </summary>
    public static class ServerCommands
    {
        public const string DumpPageInfoStore = "wikitext.server.dumpPageInfoStore";

        public static IReadOnlyCollection<string> AllCommands { get; }

        static ServerCommands()
        {
            AllCommands = typeof(ServerCommands).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).Select(f => (string) f.GetValue(null)).ToImmutableArray();
        }
    }
}
