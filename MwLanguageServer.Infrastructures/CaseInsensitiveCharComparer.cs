using System;
using System.Collections.Generic;
using System.Text;

namespace MwLanguageServer.Infrastructures
{
    public class CaseInsensitiveCharComparer : IComparer<char>
    {

        public static readonly CaseInsensitiveCharComparer Default = new CaseInsensitiveCharComparer();

        /// <inheritdoc />
        public int Compare(char x, char y)
        {
            if (char.IsLower(x)) x = char.ToUpperInvariant(x);
            if (char.IsLower(y)) y = char.ToUpperInvariant(y);
            return x.CompareTo(y);
        }
    }
}
