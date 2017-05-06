using System;
using System.Collections.Generic;
using System.Text;

namespace MwLanguageServer
{
    public class ApplicationConfiguration
    {
        public bool Debug { get; set; }

        public bool Manual { get; set; }

        public bool WaitForDebugger { get; set; }
    }
}
