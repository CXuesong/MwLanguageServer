using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using JsonRpc.Standard;
using JsonRpc.Streams;

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
                ConsoleMessageReader = new ByLineTextMessageReader(Console.In);
                ConsoleMessageWriter = new ByLineTextMessageWriter(Console.Out);
            }
            else
            {
                var cin = Console.OpenStandardInput();
                var bcin = new BufferedStream(cin);
                var cout = Console.OpenStandardOutput();
                ConsoleMessageReader = new PartwiseStreamMessageReader(bcin);
                ConsoleMessageWriter = new PartwiseStreamMessageWriter(cout);
            }
        }

        public MessageReader ConsoleMessageReader { get; }

        public MessageWriter ConsoleMessageWriter { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            ConsoleMessageReader.Dispose();
            ConsoleMessageWriter.Dispose();
        }
    }
}
