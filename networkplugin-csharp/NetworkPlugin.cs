using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
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
        public Status StatusChangedPayload { get; set; }
    }
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

    public class VCardEventArgs : EventArgs
    {
        public VCard VCardPayload { get; set; }
    }
    public delegate void VCardEventHandler(object sender, VCardEventArgs e);

    public class BuddyEventArgs : EventArgs
    {
        public Buddy BuddyPayload { get; set; }
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
            Serializer.Serialize(ms, contract);
            wrapper.payload = ms.ToArray();
            Serializer.SerializeWithLengthPrefix(Stream, wrapper, PrefixStyle.Fixed32BigEndian);
        }

        public T GetMessage<T>(byte[] payload)
        {
            return Serializer.Deserialize<T>(new MemoryStream(payload));
        }

        public event LogInEventHandler LoggedIn = delegate { };

        public event LogOutEventHandler LoggedOut = delegate { };

        public event ConversationMessageEventHandler ConversationMessage = delegate { };

        public event StatusChangedEventHandler StatusChanged = delegate { };

        public event VCardEventHandler VCardRequest = delegate { };

        public event TypingEventHandler Typing = delegate { };

        public event AttentionEventHandler Attention = delegate { };

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
                                var message =
                                    Serializer.DeserializeWithLengthPrefix<WrapperMessage>(Stream,
                                                                                           PrefixStyle.Fixed32BigEndian);
                                switch (message.type)
                                {
                                    case WrapperMessage.Type.TYPE_PING:
                                        var pong = new WrapperMessage
                                            {
                                                type = WrapperMessage.Type.TYPE_PONG,
                                                payload = null
                                            };
                                        Serializer.SerializeWithLengthPrefix(Stream, pong, PrefixStyle.Fixed32BigEndian);
                                        break;
                                    case WrapperMessage.Type.TYPE_LOGIN:
                                        var inargs = new LogInEventArgs
                                            {
                                                LoginPayload = GetMessage<Login>(message.payload)
                                            };
                                        LoggedIn(this, inargs);
                                        break;
                                    case WrapperMessage.Type.TYPE_LOGOUT:
                                        var outargs = new LogOutEventArgs
                                            {
                                                LogoutPayload = GetMessage<Logout>(message.payload)
                                            };
                                        LoggedOut(this, outargs);
                                        break;
                                    case WrapperMessage.Type.TYPE_CONV_MESSAGE:
                                        var msgargs = new ConversationMessageEventArgs
                                            {
                                                ConversationMessagePayload =
                                                    GetMessage<ConversationMessage>(message.payload)
                                            };
                                        ConversationMessage(this, msgargs);
                                        break;
                                    case WrapperMessage.Type.TYPE_STATUS_CHANGED:
                                        var statusargs = new StatusChangedEventArgs
                                            {
                                                StatusChangedPayload = GetMessage<Status>(message.payload)
                                            };
                                        StatusChanged(this, statusargs);
                                        break;
                                    case WrapperMessage.Type.TYPE_VCARD:
                                        var vcardargs = new VCardEventArgs
                                            {
                                                VCardPayload = GetMessage<VCard>(message.payload)
                                            };
                                        VCardRequest(this, vcardargs);
                                        break;
                                    case WrapperMessage.Type.TYPE_BUDDY_TYPING:
                                        var typingargs = new BuddyEventArgs
                                            {
                                                BuddyPayload = GetMessage<Buddy>(message.payload)
                                            };
                                        Typing(this, typingargs);
                                        break;
                                    case WrapperMessage.Type.TYPE_ATTENTION:
                                        var attentionargs = new ConversationMessageEventArgs
                                            {
                                                ConversationMessagePayload =
                                                    GetMessage<ConversationMessage>(message.payload)
                                            };
                                        Attention(this, attentionargs);
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
                                break;
                            }
                        }
                        _mre.Set();
                    }
                });
        }
    }
}
