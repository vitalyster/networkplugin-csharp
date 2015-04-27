using System.Diagnostics;

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
