using System;
using System.Collections.Generic;
using System.Text;

namespace MwLanguageServer
{
    /// <summary>
    /// Sort template arguments so that argument parameters are always before named ones.
    /// </summary>
    public class TemplateArgumentNameComparer : IComparer<string>
    {

        public static readonly TemplateArgumentNameComparer Default = new TemplateArgumentNameComparer();

        /// <inheritdoc />
        public int Compare(string x, string y)
        {
            // Need stable sort.
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return 1;
            var si = int.TryParse(x, out var i);
            var sj = int.TryParse(y, out var j);
            if (si && sj) return i.CompareTo(j);
            else if (si) return -1;
            else if (sj) return 1;
            else return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }
}
