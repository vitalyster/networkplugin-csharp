#define TRACE

using System;
using System.Diagnostics;
using System.IO;
using ProtoBuf;
using pbnetwork;
using networkplugin_csharp;

namespace MSNBackend
{

    public static class Program
    {
        
        public static void Main(string[] args)
        {
            //Trace.Listeners.Add(new ConsoleTraceListener(true));            
            var host = args[1];
            var port = args[3];
            var backend = new MSNPlugin(host, port);
            backend.Connect();
            backend.Loop();
        }

    }
}
