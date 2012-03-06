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

    public class StatusChangedEventArgs : EventArgs
    {
        public pbnetwork.Status StatusChangedPayload { get; set; }
    }
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

    public class VCardEventArgs : EventArgs
    {
        public pbnetwork.VCard VCardPayload { get; set; }
    }
    public delegate void VCardEventHandler(object sender, VCardEventArgs e);

    public class BuddyEventArgs : EventArgs
    {
        public pbnetwork.Buddy BuddyPayload { get; set; }
    }
    public delegate void TypingEventHandler(object sender, BuddyEventArgs e);
    public delegate void AttentionEventHandler(object sender, ConversationMessageEventArgs e);

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

        public T GetMessage<T>(byte[] payload)
        {
            return Serializer.Deserialize<T>(new MemoryStream(payload));
        }

        public event LogInEventHandler LoggedIn;

        private void OnLoggedIn(LogInEventArgs e)
        {
            LogInEventHandler handler = LoggedIn;
            if (handler != null) handler(this, e);
        }

        public event LogOutEventHandler LoggedOut;

        private void OnLoggedOut(LogOutEventArgs e)
        {
            LogOutEventHandler handler = LoggedOut;
            if (handler != null) handler(this, e);
        }

        public event ConversationMessageEventHandler ConversationMessage;

        private void OnConversationMessage(ConversationMessageEventArgs e)
        {
            ConversationMessageEventHandler handler = ConversationMessage;
            if (handler != null) handler(this, e);
        }

        public event StatusChangedEventHandler StatusChanged;

        private void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChangedEventHandler handler = StatusChanged;
            if (handler != null) handler(this, e);
        }

        public event VCardEventHandler VCardRequest;

        private void OnVCard(VCardEventArgs e)
        {
            VCardEventHandler handler = VCardRequest;
            if (handler != null) handler(this, e);
        }

        public event TypingEventHandler Typing;

        private void OnTyping(BuddyEventArgs e)
        {
            TypingEventHandler handler = Typing;
            if (handler != null) handler(this, e);
        }

        public event AttentionEventHandler Attention;

        private void OnAttention(ConversationMessageEventArgs e)
        {
            AttentionEventHandler handler = Attention;
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
                                               var cont = true;
                                               while (cont)
                                               {
                                                   try
                                                   {
                                                       var message = Serializer.DeserializeWithLengthPrefix<WrapperMessage>(Stream,
                                                                                                                            PrefixStyle.
                                                                                                                                Fixed32BigEndian);
                                                       switch (message.type)
                                                       {
                                                           case WrapperMessage.Type.TYPE_PING:
                                                               var pong = new WrapperMessage { type = WrapperMessage.Type.TYPE_PONG, payload = null };
                                                               Serializer.SerializeWithLengthPrefix<WrapperMessage>(Stream, pong, PrefixStyle.Fixed32BigEndian);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_LOGIN:
                                                               var inargs = new LogInEventArgs
                                                                                   {
                                                                                       LoginPayload = GetMessage<Login>(message.payload)
                                                                                   };
                                                               OnLoggedIn(inargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_LOGOUT:
                                                               var outargs = new LogOutEventArgs
                                                                                   {
                                                                                       LogoutPayload = GetMessage<Logout>(message.payload)
                                                                                   };
                                                               OnLoggedOut(outargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_CONV_MESSAGE:
                                                               var msgargs = new ConversationMessageEventArgs
                                                                                   {
                                                                                       ConversationMessagePayload = GetMessage<ConversationMessage>(message.payload)
                                                                                   };
                                                               OnConversationMessage(msgargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_STATUS_CHANGED:
                                                               var statusargs = new StatusChangedEventArgs
                                                                                   {
                                                                                       StatusChangedPayload = GetMessage<Status>(message.payload)
                                                                                   };
                                                               OnStatusChanged(statusargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_VCARD:
                                                               var vcardargs = new VCardEventArgs
                                                                                   {
                                                                                       VCardPayload = GetMessage<VCard>(message.payload)
                                                                                   };
                                                               OnVCard(vcardargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_BUDDY_TYPING:
                                                               var typingargs = new BuddyEventArgs
                                                                                   {
                                                                                       BuddyPayload = GetMessage<Buddy>(message.payload)
                                                                                   };
                                                               OnTyping(typingargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_ATTENTION:
                                                               var attentionargs = new ConversationMessageEventArgs
                                                                                   {
                                                                                       ConversationMessagePayload = GetMessage<ConversationMessage>(message.payload)
                                                                                   };
                                                               OnAttention(attentionargs);
                                                               break;
                                                           case WrapperMessage.Type.TYPE_EXIT:
                                                               Trace.WriteLine("Normal shutdown");
                                                               cont = false;
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
