using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MwLanguageServer
{
    public sealed class SynchronizedTextWriter : TextWriter
    {
        private readonly TextWriter _out;
        private readonly object syncLock = new object();

        public SynchronizedTextWriter(TextWriter t) : base(t.FormatProvider)
        {
            _out = t;
        }

        public override Encoding Encoding => _out.Encoding;

        public override IFormatProvider FormatProvider => _out.FormatProvider;

        public override string NewLine
        {
            get
            {
                lock (syncLock) return _out.NewLine;
            }
            set
            {
                lock (syncLock) _out.NewLine = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Explicitly pick up a potentially methodimpl'ed Dispose
            if (disposing)
            {
                lock (syncLock)
                    ((IDisposable) _out).Dispose();
            }
        }


        public override void Flush()
        {
            lock (syncLock) _out.Flush();
        }


        public override void Write(char value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(char[] buffer)
        {
            lock (syncLock) _out.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            lock (syncLock) _out.Write(buffer, index, count);
        }


        public override void Write(bool value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(int value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(uint value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(long value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(ulong value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(float value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(double value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(decimal value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(string value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(object value)
        {
            lock (syncLock) _out.Write(value);
        }


        public override void Write(string format, object arg0)
        {
            lock (syncLock) _out.Write(format, arg0);
        }


        public override void Write(string format, object arg0, object arg1)
        {
            lock (syncLock) _out.Write(format, arg0, arg1);
        }


        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            lock (syncLock) _out.Write(format, arg0, arg1, arg2);
        }


        public override void Write(string format, object[] arg)
        {
            lock (syncLock) _out.Write(format, arg);
        }


        public override void WriteLine()
        {
            lock (syncLock) _out.WriteLine();
        }


        public override void WriteLine(char value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(decimal value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(char[] buffer)
        {
            lock (syncLock) _out.WriteLine(buffer);
        }


        public override void WriteLine(char[] buffer, int index, int count)
        {
            lock (syncLock) _out.WriteLine(buffer, index, count);
        }


        public override void WriteLine(bool value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(int value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(uint value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(long value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(ulong value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(float value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(double value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(string value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(object value)
        {
            lock (syncLock) _out.WriteLine(value);
        }


        public override void WriteLine(string format, object arg0)
        {
            lock (syncLock) _out.WriteLine(format, arg0);
        }


        public override void WriteLine(string format, object arg0, object arg1)
        {
            lock (syncLock) _out.WriteLine(format, arg0, arg1);
        }


        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            lock (syncLock) _out.WriteLine(format, arg0, arg1, arg2);
        }


        public override void WriteLine(string format, object[] arg)
        {
            lock (syncLock) _out.WriteLine(format, arg);
        }

//
// On SyncTextWriter all APIs should run synchronously, even the async ones.
//


        [ComVisible(false)]
        public override Task WriteAsync(char value)
        {
            Write(value);
            return Task.CompletedTask;
        }


        [ComVisible(false)]
        public override Task WriteAsync(string value)
        {
            Write(value);
            return Task.CompletedTask;
        }


        [ComVisible(false)]
        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            return Task.CompletedTask;
        }


        [ComVisible(false)]
        public override Task WriteLineAsync(char value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }


        [ComVisible(false)]
        public override Task WriteLineAsync(string value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }


        [ComVisible(false)]
        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            WriteLine(buffer, index, count);
            return Task.CompletedTask;
        }


        [ComVisible(false)]
        public override Task FlushAsync()
        {
            Flush();
            return Task.CompletedTask;
        }
    }
}
