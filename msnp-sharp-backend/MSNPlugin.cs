using System.Diagnostics;
using System.IO;
using ProtoBuf;
using networkplugin_csharp;
using pbnetwork;
using System.Collections.Generic;
using MSNPSharp;
using MSNPSharp.Apps;
using MSNPSharp.Core;
using MSNPSharp.P2P;
using MSNPSharp.MSNWS.MSNABSharingService;
using MSNPSharp.IO;

namespace MSNBackend
{
    public class MSNPlugin : NetworkPlugin
    {
		private Dictionary<string, MSNMessenger> messengers;

        private void HandleLogin(object sender, LogInEventArgs login)
        {

			MSNMessenger messenger = new MSNMessenger(this, login.LoginPayload.user, login.LoginPayload.legacyName, login.LoginPayload.password);
			messengers.Add(login.LoginPayload.user, messenger);
        }

        private void HandleLogout(object sender, LogOutEventArgs logout)
        {
            messengers[logout.LogoutPayload.user].Disconnect();
        }
		
		private void HandleConversationMessage(object sender, ConversationMessageEventArgs message)
		{
			MSNMessenger messenger = messengers[message.ConversationMessagePayload.userName];
			Contact contact = messenger.ContactList.GetContact(message.ConversationMessagePayload.buddyName);
			messenger.SendTextMessage(contact, message.ConversationMessagePayload.message);
		}

        public MSNPlugin(string host, string port) : base(host, port)
        {
			messengers = new Dictionary<string, MSNMessenger>();
            LoggedIn += HandleLogin;
            LoggedOut += HandleLogout;
			ConversationMessage += HandleConversationMessage;
        }
    
    }
}

