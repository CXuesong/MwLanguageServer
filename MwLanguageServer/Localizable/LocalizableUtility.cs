using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MwLanguageServer.Localizable
{
    public static class LocalizableUtility
    {

        public static string ExpandPath(string path)
        {
            return Path.GetFullPath(Path.Combine("Localizable/Content", path));
        }

        public static TextReader OpenTextReader(string path)
        {
            return File.OpenText(ExpandPath(path));
        }
    }
}
