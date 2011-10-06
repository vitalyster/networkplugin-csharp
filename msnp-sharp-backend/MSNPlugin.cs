using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
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
		public Dictionary<string, MSNMessenger> messengers;

        private void HandleLogin(object sender, LogInEventArgs login)
        {

			MSNMessenger messenger = new MSNMessenger(this, login.LoginPayload.user, login.LoginPayload.legacyName, login.LoginPayload.password);
			messengers.Add(login.LoginPayload.user, messenger);
        }

        private void HandleLogout(object sender, LogOutEventArgs logout)
        {
			if (messengers.ContainsKey(logout.LogoutPayload.user))
            	messengers[logout.LogoutPayload.user].Disconnect();
        }
		
		private void HandleConversationMessage(object sender, ConversationMessageEventArgs message)
		{
			MSNMessenger messenger = messengers[message.ConversationMessagePayload.userName];
			Contact contact = messenger.ContactList.GetContact(message.ConversationMessagePayload.buddyName);
			messenger.SendTextMessage(contact, message.ConversationMessagePayload.message);
		}
		
		private void HandleStatusChanged(object sender, networkplugin_csharp.StatusChangedEventArgs status)
		{
			MSNMessenger messenger = messengers[status.StatusChangedPayload.userName];
			messenger.setStatus(messenger.PluginStatusToPresenceStatus(status.StatusChangedPayload.status));
		}

		public byte[] imageToByteArray(Image imageIn)
		{
			MemoryStream ms = new MemoryStream();
			imageIn.Save(ms,System.Drawing.Imaging.ImageFormat.Gif);
			return  ms.ToArray();
		}
		
		public Image byteArrayToImage(byte[] byteArrayIn)
		{
		     MemoryStream ms = new MemoryStream(byteArrayIn);
		     Image returnImage = Image.FromStream(ms);
		     return returnImage;
		}

		void HandleVCardRequest(object sender, VCardEventArgs vcard)
        {
        	MSNMessenger messenger = messengers[vcard.VCardPayload.userName];
			if (vcard.VCardPayload.photo == null) {
				if (messenger.Owner.Account == vcard.VCardPayload.buddyName) {
		            var response = new VCard { userName = messenger.user, buddyName = messenger.Owner.Account, id = vcard.VCardPayload.id,
												nickname = messenger.Owner.PreferredName, fullname = messenger.Owner.Name,
						 photo = messenger.Owner.DisplayImage != null && messenger.Owner.DisplayImage.Image != null ? imageToByteArray(messenger.Owner.DisplayImage.Image) : new byte[0]};
		            SendMessage(WrapperMessage.Type.TYPE_VCARD, response);
				}
				else {
					Contact contact = messenger.ContactList.GetContact(vcard.VCardPayload.buddyName);
					if (contact != null) {
			            var response = new VCard { userName = messenger.user, buddyName = vcard.VCardPayload.buddyName, id = vcard.VCardPayload.id,
									nickname = contact.PreferredName, fullname = contact.Name,
						 	photo = contact.DisplayImage != null && contact.DisplayImage.Image != null ? imageToByteArray(contact.DisplayImage.Image) : new byte[0]};
		            	SendMessage(WrapperMessage.Type.TYPE_VCARD, response);
					}
				}
			}
			else {
				var newImage = byteArrayToImage(vcard.VCardPayload.photo);
	            messenger.Owner.UpdateDisplayImage(newImage);
                messenger.Owner.UpdateRoamingProfileSync(newImage);
			}
			
        }

        public MSNPlugin(string host, string port) : base(host, port)
        {
			messengers = new Dictionary<string, MSNMessenger>();
            LoggedIn += HandleLogin;
            LoggedOut += HandleLogout;
			ConversationMessage += HandleConversationMessage;
			StatusChanged += HandleStatusChanged;
			VCardRequest += HandleVCardRequest;
			Typing += HandleTyping;
			Attention += HandleAttention;
        }

        void HandleAttention (object sender, ConversationMessageEventArgs e)
        {
        	MSNMessenger messenger = messengers[e.ConversationMessagePayload.userName];
			Contact contact = messenger.ContactList.GetContact(e.ConversationMessagePayload.buddyName);
			if (contact != null) {
				messenger.SendNudge(contact);
			}
        }

        void HandleTyping (object sender, BuddyEventArgs e)
        {
        	MSNMessenger messenger = messengers[e.BuddyPayload.userName];
			Contact contact = messenger.ContactList.GetContact(e.BuddyPayload.buddyName);
			if (contact != null) {
				messenger.SendTypingMessage(contact);
			}
        }
    
    }
}

