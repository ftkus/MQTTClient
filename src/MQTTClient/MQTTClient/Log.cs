using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTClient
{
    internal class Log
    {
        internal class LogEventArgs : EventArgs
        {
            public LogEventArgs(Log log)
            {
                Log = log;
            }

            internal Log Log { get; }
        }

        internal event EventHandler<LogEventArgs> Updated;

        private readonly List<string> log = new List<string>();

        internal void Add(string message)
        {
            log.Add(message);

            Updated?.Invoke(this, new LogEventArgs(this));
        }

        public override string ToString()
        {
            using (StringWriter sw = new StringWriter())
            {
                foreach (var l in log)
                {
                    sw.WriteLine(l);
                }

                return sw.ToString().Trim();
            }
        }

    }
}
