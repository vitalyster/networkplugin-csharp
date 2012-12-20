using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace spectrum2_sharposcar_backend
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener(true));
            var host = args[1];
            var port = args[3];
            var backend = new OscarPlugin(host, port);
            backend.Connect();
            backend.Loop();
        }
    }
}
