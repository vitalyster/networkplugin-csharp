using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using ARSoft.Tools.Net.Dns;
using networkplugin_csharp;
using pbnetwork;
using S22.Imap;

namespace spectrum2_imap_backend
{
    public class IMAPPlugin : NetworkPlugin
    {
        readonly Dictionary<string, ImapClient> _sessions; 

        public IMAPPlugin(string host, string port) : base(host, port)
        {
            _sessions = new Dictionary<string, ImapClient>();
            
            LoggedIn += (sender, login) =>
            {
                var address = new MailAddress(login.LoginPayload.legacyName);
                var service = DnsClient.Default.Resolve(string.Format("_imaps._tcp.{0}", address.Host), RecordType.Srv)
                    .AnswerRecords.OfType<SrvRecord>().First();
                var newClient = new ImapClient(service.Target, service.Port, 
                    login.LoginPayload.legacyName, login.LoginPayload.password, AuthMethod.Auto, true);
                SendMessage(WrapperMessage.Type.TYPE_CONNECTED, new Connected {user = login.LoginPayload.user});
                SendMessage(WrapperMessage.Type.TYPE_BUDDY_CHANGED, new Buddy
                {
                    userName = login.LoginPayload.user,
                    buddyName = address.Host,
                    status = StatusType.STATUS_ONLINE
                });
                newClient.NewMessage += (o, args) =>
                {
                    var msg = args.Client.GetMessage(args.MessageUID, FetchOptions.HeadersOnly).Subject;
                    SendMessage(WrapperMessage.Type.TYPE_CONV_MESSAGE, new ConversationMessage
                    {
                        userName = login.LoginPayload.user,
                        buddyName = address.Host,
                        headline = true,
                        message = msg
                    });
                };
                _sessions.Add(login.LoginPayload.user, newClient);
            };

            LoggedOut += (sender, args) =>
            {
                var user = args.LogoutPayload.user;
                if (!_sessions.ContainsKey(user)) return;
                _sessions[user].Logout();
                _sessions.Remove(user);
            };
        }
    }
}
