#define TRACE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ProtoBuf;
using pbnetwork;
namespace NetworkPlugin
{

    public static class Program
    {

        public static void SendPong(NetworkStream stream)
        {
            var message = new WrapperMessage {type = WrapperMessage.Type.TYPE_PONG, payload = null};
            Serializer.SerializeWithLengthPrefix<WrapperMessage>(stream, message, PrefixStyle.Fixed32BigEndian);
        }

        public static void HandleLogin(NetworkStream stream, Login login)
        {

            Trace.WriteLine(string.Format("Login: {0}, {1}, {2}", login.user, login.password, login.legacyName));

            var connected = new Connected {user = login.user};
            var wrapper = new WrapperMessage {type = WrapperMessage.Type.TYPE_CONNECTED};
            var ms = new MemoryStream();
            Serializer.Serialize<Connected>(ms, connected);
            wrapper.payload = ms.ToArray();
            Serializer.SerializeWithLengthPrefix<WrapperMessage>(stream, wrapper, PrefixStyle.Fixed32BigEndian);
        }

        public static void HandleLogout(NetworkStream stream, Logout logout)
        {
            Trace.WriteLine(string.Format("Logout: {0}", logout.user));
        }
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener(true));            
            var host = args[1];
            var port = int.Parse(args[3]);
            using (var client = new TcpClient())
            {
                client.Connect(host, port);
                Trace.WriteLine("Backend connected");
                var networkStream = client.GetStream();
                while (true)
                {
                    try
                    {
                        var message = Serializer.DeserializeWithLengthPrefix<WrapperMessage>(networkStream,
                                                                                             PrefixStyle.
                                                                                                 Fixed32BigEndian);
                        switch (message.type)
                        {
                            case WrapperMessage.Type.TYPE_PING:
                                SendPong(networkStream);
                                break;
                            case WrapperMessage.Type.TYPE_LOGIN:
                                HandleLogin(networkStream,
                                            Serializer.Deserialize<Login>(new MemoryStream(message.payload)));
                                break;
                            case WrapperMessage.Type.TYPE_LOGOUT:
                                HandleLogout(networkStream,
                                             Serializer.Deserialize<Logout>(new MemoryStream(message.payload)));
                                break;
                            default:
                                Trace.WriteLine("Unhandled packet: " + message.type.ToString());
                                break;
                        }
                    } catch (IOException e)
                    {
                        Trace.WriteLine("SocketException: " + e.Message);
                        break;
                    }
                }
                
            }
        }
    }
}
