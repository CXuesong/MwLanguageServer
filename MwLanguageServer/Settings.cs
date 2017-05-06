using System;
using System.Collections.Generic;
using System.Text;

namespace MwLanguageServer
{

    public class SettingsRoot
    {

        public LanguageServerSettings WikitextLanguageServer { get; set; }

    }

    public class LanguageServerSettings
    {
        public int MaxNumberOfProblems { get; set; } = 10;

        public LanguageServerTraceSettings Trace { get; } = new LanguageServerTraceSettings();
    }

    public class LanguageServerTraceSettings
    {
        public string Server { get; set; }
    }
}
