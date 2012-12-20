using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IcqSharp;
using IcqSharp.Base;
using networkplugin_csharp;
using pbnetwork;
using Status = IcqSharp.Base.Status;

namespace spectrum2_sharposcar_backend
{
    public class OscarPlugin : NetworkPlugin
    {
        public Dictionary<string, Session> OscarSessions;
        
        public OscarPlugin(string host, string port) : base(host, port)
        {
            OscarSessions = new Dictionary<string, Session>();
            LoggedIn += (sender, login) =>
                {
                    var newSession = new Session(login.LoginPayload.legacyName, login.LoginPayload.password);
                    OscarSessions.Add(login.LoginPayload.user, newSession);
                    newSession.Messaging.MessageReceived += message =>
                        {
                            var currentUser = OscarSessions.FirstOrDefault(k => k.Value == newSession).Key;
                            if (currentUser == null) return;
                            var convMessage = new ConversationMessage
                                {
                                    userName = currentUser,
                                    buddyName = message.Contact.Uin,
                                    message = message.Text
                                };
                            SendMessage(WrapperMessage.Type.TYPE_CONV_MESSAGE, convMessage);
                        };
                    newSession.Connected += (o, args) =>
                        {
                            var currentUser = OscarSessions.FirstOrDefault(k => k.Value == newSession).Key;
                            if (currentUser == null) return;
                            var connected = new Connected {user = currentUser};
                            SendMessage(WrapperMessage.Type.TYPE_CONNECTED, connected);
                        };
                    newSession.ContactList.ContactSignedOn += contact => ContactChanged(newSession, contact);
                    newSession.ContactList.ContactSignedOff += contact => ContactChanged(newSession, contact);
                    newSession.ContactList.ContactRemoved += contact => ContactChanged(newSession, contact);
                    newSession.ContactList.ContactAdded += contact => ContactChanged(newSession, contact);
                    newSession.ContactList.ContactStatusChanged += contact => ContactChanged(newSession, contact);
                    newSession.ContactList.ContactListReceived +=
                        (o, args) => newSession.ContactList.Contacts.ForEach(c => ContactChanged(newSession, c));
                    newSession.Connect();
                };
            LoggedOut += (sender, logout) =>
                {
                    if (!OscarSessions.ContainsKey(logout.LogoutPayload.user)) return;
                    OscarSessions[logout.LogoutPayload.user].Disconnect();
                    OscarSessions[logout.LogoutPayload.user].Dispose();
                    OscarSessions.Remove(logout.LogoutPayload.user);
                };
            ConversationMessage += (sender, message) =>
                {
                    var session = OscarSessions[message.ConversationMessagePayload.userName];
                    var contact = session.ContactList.Contacts.FirstOrDefault(
                        c => c.Uin.Equals(message.ConversationMessagePayload.buddyName));
                    if (contact == null) return;
                    /*var datetime =
                        (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(
                            long.Parse(message.ConversationMessagePayload.timestamp));*/
                    session.Messaging.Send(new Message(contact, MessageType.Outgoing,
                                                       //datetime,
                                                       message.ConversationMessagePayload.message));
                };
        }

        private void ContactChanged(Session session, Contact contact)
        {
            var currentUser = OscarSessions.FirstOrDefault(k => k.Value == session).Key;
            if (currentUser == null) return;
            var changedBuddy = new Buddy
                {
                    userName = currentUser,
                    buddyName = contact.Uin,
                    alias = contact.Nickname,
                    status = OscarStatusTypeToPluginType(contact.Status)
                };
            SendMessage(WrapperMessage.Type.TYPE_BUDDY_CHANGED, changedBuddy);
        }

        private static StatusType OscarStatusTypeToPluginType(Status status)
        {
            switch (status)
            {
                case Status.Offline:
                    return StatusType.STATUS_NONE;
                case Status.Online:
                    return StatusType.STATUS_ONLINE;
                case Status.Away:
                    return StatusType.STATUS_AWAY;
                case Status.DoNotDisturb:
                    return StatusType.STATUS_DND;
                default:
                    return StatusType.STATUS_NONE;
            }
        }

        public Status PluginStatusToOscarStatus(StatusType status)
        {
            switch (status)
            {
                case StatusType.STATUS_NONE:
                    return Status.Offline;
                case StatusType.STATUS_AWAY:
                    return Status.Away;
                case StatusType.STATUS_DND:
                    return Status.DoNotDisturb;
                default:
                    return Status.Online;
            }
        }
    }
}
