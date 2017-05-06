using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MwLanguageServer
{
    public class ServiceHostLifetime : IDisposable
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public CancellationToken CancellationToken => cts.Token;

        public void Stop()
        {
            cts.Cancel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            cts.Dispose();
        }
    }
}
