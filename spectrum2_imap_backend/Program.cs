using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace spectrum2_imap_backend
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener(true));
            var host = args[1];
            var port = args[3];
            var backend = new IMAPPlugin(host, port);
            backend.Connect();
            backend.Loop();
        }
    }
}
