using System;
using System.Collections.Generic;
using System.Text;

namespace MwLanguageServer
{
    public class ApplicationConfiguration
    {
        public bool Debug { get; set; }

        /// <summary>
        /// Verbose logging.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Whether to simply read messages from console, line by line,
        /// instead of using language server protocol's specification.
        /// </summary>
        public bool Manual { get; set; }

        public bool WaitForDebugger { get; set; }

        public string Language { get; set; }
    }
}
