#define TRACE

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using ProtoBuf;
using pbnetwork;
using networkplugin_csharp;

namespace networkplugin_demo
{

    public static class Program
    {
        
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener(true));            
            var host = args[1];
            var port = args[3];
            var backend = new DumbPlugin(host, port);
            backend.Connect();
            backend.Loop();
        }

    }
}
