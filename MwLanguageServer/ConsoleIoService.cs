using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks.Dataflow;
using JsonRpc.Standard;
using JsonRpc.Standard.Dataflow;

namespace MwLanguageServer
{
    public sealed class ConsoleIoService : IDisposable
    {
        /// <param name="manualIo">Simply read messages from console, line by line,
        /// instead of using language server protocol's specification.</param>
        public ConsoleIoService(bool manualIo)
        {
            if (manualIo)
            {
                ConsoleMessageSource = new ByLineTextMessageSourceBlock(Console.In);
                ConsoleMessageTarget = new ByLineTextMessageTargetBlock(Console.Out);
                ConsoleIn = null;
                ConsoleOut = null;
            }
            else
            {
                var cin = Console.OpenStandardInput();
                ConsoleIn = new BufferedStream(cin);
                ConsoleOut = Console.OpenStandardOutput();
                ConsoleMessageSource = new PartwiseStreamMessageSourceBlock(ConsoleIn);
                ConsoleMessageTarget = new PartwiseStreamMessageTargetBlock(ConsoleOut);
            }
        }

        private Stream ConsoleIn { get; }

        private Stream ConsoleOut { get; }

        public ISourceBlock<Message> ConsoleMessageSource { get; }

        public ITargetBlock<Message> ConsoleMessageTarget { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            ConsoleMessageSource.Complete();
            ConsoleMessageTarget.Complete();
            ConsoleIn?.Dispose();
            ConsoleOut?.Dispose();
        }
    }
}
