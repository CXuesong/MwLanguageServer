using System;
using System.IO;
using Serilog;
using Serilog.Core;
using VSCode;
using VSCode.Editor;

namespace MwLanguageServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var logWriter = File.CreateText("MwLanguageServer-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"))
            using (var server = new LanguageServer())
            {
                var logger = new LoggerConfiguration().WriteTo.TextWriter(logWriter)
                    .CreateLogger();
                try
                {
                    server.Start();
                    server.WaitForState(LanguageServerState.Started);
                    server.Editor.ShowMessage(MessageType.Info, "Hello from .NET!");
                }
                catch (Exception e)
                {
                    logger.Error(e, "Critial exception.");
                }
            }
        }
    }
}