using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ProtoBuf;
using pbnetwork;

namespace networkplugin_csharp
{
    public class LogInEventArgs : EventArgs
    {
        public Login LoginPayload { get; set; }
    }
    public delegate void LogInEventHandler(object sender, LogInEventArgs e);

    public class LogOutEventArgs : EventArgs
    {
        public Logout LogoutPayload { get; set; }
    }
    public delegate void LogOutEventHandler(object sender, LogOutEventArgs e);

    public class ConversationMessageEventArgs : EventArgs
    {
        public ConversationMessage ConversationMessagePayload { get; set; }
    }
    public delegate void ConversationMessageEventHandler(object sender, ConversationMessageEventArgs e);

    public class NetworkPlugin
    {
        public string Host { get; set; }
        public string Port { get; set; }

        protected NetworkStream Stream { get; private set; }

    public void SendMessage<T>(WrapperMessage.Type type, T contract) 
    {
        var wrapper = new WrapperMessage { type = type };
        var ms = new MemoryStream();
        Serializer.Serialize<T>(ms, contract);
        wrapper.payload = ms.ToArray();
        Serializer.SerializeWithLengthPrefix<WrapperMessage>(Stream, wrapper, PrefixStyle.Fixed32BigEndian);
    }

        public event LogInEventHandler LoggedIn;

        public void OnLoggedIn(LogInEventArgs e)
        {
            LogInEventHandler handler = LoggedIn;
            if (handler != null) handler(this, e);
        }

        public event LogOutEventHandler LoggedOut;

        public void OnLoggedOut(LogOutEventArgs e)
        {
            LogOutEventHandler handler = LoggedOut;
            if (handler != null) handler(this, e);
        }
		
		public event ConversationMessageEventHandler ConversationMessage;

        public void OnConversationMessage(ConversationMessageEventArgs e)
        {
            ConversationMessageEventHandler handler = ConversationMessage;
            if (handler != null) handler(this, e);
        }

        public NetworkPlugin(string host, string port)
        {
            Host = host;
            Port = port;
        }

        private ManualResetEvent _mre;

        public void Loop()
        {
            _mre.WaitOne();
        }

        public void Connect()
        {
            _mre = new ManualResetEvent(false);


            ThreadPool.QueueUserWorkItem((object stateInfo) =>
                                       {
                                           using (var client = new TcpClient())
                                           {
                                               client.Connect(Host, int.Parse(Port));
                                               Trace.WriteLine("Backend connected");
                                               Stream = client.GetStream();
                                               while (true)
                                               {
                                                   try
                                                   {
                                                       var message = Serializer.DeserializeWithLengthPrefix<WrapperMessage>(Stream,
                                                                                                                            PrefixStyle.
                                                                                                                                Fixed32BigEndian);
                                                       switch (message.type)
                                                       {
                                                           case WrapperMessage.Type.TYPE_PING:
                                                               var pong = new WrapperMessage {type = WrapperMessage.Type.TYPE_PONG, payload = null};
                                                               Serializer.SerializeWithLengthPrefix<WrapperMessage>(Stream, pong, PrefixStyle.Fixed32BigEndian);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_LOGIN:
                                                               var inargs = new LogInEventArgs
                                                                                   {
                                                                                       LoginPayload =
                                                                                           Serializer.Deserialize<Login>
                                                                                           (
                                                                                               new MemoryStream(
                                                                                                   message.payload))
                                                                                   };
                                                               OnLoggedIn(inargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_LOGOUT:
                                                               var outargs = new LogOutEventArgs
                                                                                   {
                                                                                       LogoutPayload =
                                                                                           Serializer.Deserialize<Logout>
                                                                                           (
                                                                                               new MemoryStream(
                                                                                                   message.payload))
                                                                                   };
                                                               OnLoggedOut(outargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_CONV_MESSAGE:
                                                               var msgargs = new ConversationMessageEventArgs
                                                                                   {
                                                                                       ConversationMessagePayload =
                                                                                           Serializer.Deserialize<ConversationMessage>
                                                                                           (
                                                                                               new MemoryStream(
                                                                                                   message.payload))
                                                                                   };
                                                               OnConversationMessage(msgargs);
                                                               break;
                                                           default:
                                                               Trace.WriteLine("Unhandled packet: " + message.type.ToString());
                                                               break;
                                                       }
                                                   }
                                                   catch (IOException e)
                                                   {
                                                       Trace.WriteLine("SocketException: " + e.Message);
                                                       _mre.Set();
                                                       break;
                                                   }
                                               }

                                           }
                                       });
        }
    }
}
