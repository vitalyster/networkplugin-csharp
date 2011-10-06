using System;
using MSNPSharp;
using MSNPSharp.Apps;
using MSNPSharp.Core;
using MSNPSharp.P2P;
using MSNPSharp.MSNWS.MSNABSharingService;
using MSNPSharp.IO;
using pbnetwork;

namespace MSNBackend
{
	public class MSNMessenger : Messenger
	{
		public string user;
		private MSNPlugin plugin;
		private PresenceStatus st;

		public MSNMessenger(MSNPlugin plugin, string user, string legacyName, string password)
		{
			Console.WriteLine("AAAAAAA2");
			this.plugin = plugin;
			this.user = user;
			
			Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);
			Nameserver.ContactOnline += new EventHandler<ContactStatusChangedEventArgs>(Nameserver_ContactOnline);
			Nameserver.ContactOffline += new EventHandler<ContactStatusChangedEventArgs>(Nameserver_ContactOnline);
            MessageManager.TypingMessageReceived += new EventHandler<TypingArrivedEventArgs>(Nameserver_TypingMessageReceived);
            MessageManager.TextMessageReceived += new EventHandler<TextMessageArrivedEventArgs>(Nameserver_TextMessageReceived);
			MessageManager.NudgeReceived += new EventHandler<NudgeArrivedEventArgs>(MessageManager_NudgeReceived);
			ContactService.ContactAdded += new EventHandler<ListMutateEventArgs>(ContactService_ContactAdded);
			Nameserver.AutoSynchronize = true;
			
			Credentials = new Credentials(legacyName, password);
			Connect();
		}

		private void MessageManager_NudgeReceived(object sender, NudgeArrivedEventArgs e) {
			var message = new ConversationMessage {userName = this.user, buddyName = e.Sender.Account, message = "" };
			plugin.SendMessage(WrapperMessage.Type.TYPE_ATTENTION, message);
		}
		
        private void Nameserver_TextMessageReceived(object sender, TextMessageArrivedEventArgs e)
        {
			var message = new ConversationMessage {userName = this.user, buddyName = e.Sender.Account, message = e.TextMessage.Text};
			plugin.SendMessage(WrapperMessage.Type.TYPE_CONV_MESSAGE, message);
        }

        private void Nameserver_TypingMessageReceived(object sender, TypingArrivedEventArgs e)
        {
			var message = new Buddy {userName = this.user, buddyName = e.Sender.Account};
			plugin.SendMessage(WrapperMessage.Type.TYPE_BUDDY_TYPING, message);
        }
		
		private StatusType MSNStatusTypeToPluginType(PresenceStatus status)
		{
			switch(status) {
				case PresenceStatus.Offline:
					return StatusType.STATUS_NONE;
				case PresenceStatus.Online:
					return StatusType.STATUS_ONLINE;
				case PresenceStatus.Away:
					return StatusType.STATUS_AWAY;
				default:
					return StatusType.STATUS_NONE;
			}
		}

		public PresenceStatus PluginStatusToPresenceStatus(StatusType status)
		{
			switch(status) {
				case StatusType.STATUS_AWAY:
					return PresenceStatus.Away;
				default:
					return PresenceStatus.Online;
			}
		}

		public void setStatus(PresenceStatus stat) {
			st = stat;
			if (this.Connected) {
				Owner.Status = st;
			}
		}

		private void ContactService_ContactAdded(object sender, ListMutateEventArgs e)
		{
		}
	
		private void ContactChanged(Contact contact) {
			var buddy = new Buddy {userName = this.user, buddyName = contact.Account, alias = contact.PreferredName,
			                        groups = contact.ContactGroups.Count == 0 ? "Buddies" : contact.ContactGroups[0].ToString(), status = MSNStatusTypeToPluginType(contact.Status) };
			if (contact.DisplayImage != null && contact.DisplayImage.Image != null) {
				buddy.iconHash = contact.DisplayImage.Sha;
			}
			buddy.statusMessage = contact.PersonalMessage.Message;
			plugin.SendMessage(WrapperMessage.Type.TYPE_BUDDY_CHANGED, buddy);
		}

		private void DownloadedDisplayImage(object sender, ObjectEventArgs e) {
			Console.WriteLine("DownloadedDisplayImage");
		}
		
		private void Nameserver_ContactOnline(object sender, ContactStatusChangedEventArgs e)
		{
            if (e.Contact.Status != PresenceStatus.Offline)
            {
                if (e.Contact.DisplayImage != e.Contact.UserTileLocation)
                {
	                //RequestDisplayImage(e.Contact, null);
				}
			}
			
			if (e.Contact.UserTileURL != null && e.Contact.ClientType != IMAddressInfoType.WindowsLive) {
				HttpAsyncDataDownloader.BeginDownload(e.Contact.UserTileURL.AbsoluteUri, new EventHandler<ObjectEventArgs>(DownloadedDisplayImage), null);
			}
			
			ContactChanged(e.Contact);
		}

        private void RequestDisplayImage(Contact remoteContact, DisplayImage updateImage)
        {
            if (remoteContact.P2PVersionSupported != P2PVersion.None && 
                remoteContact.ClientType == IMAddressInfoType.WindowsLive &&
                updateImage != remoteContact.UserTileLocation)
            {
                if (updateImage == null)
                    updateImage = remoteContact.DisplayImage;

                RequestMsnObject(remoteContact, updateImage);
            }
        }

		private void Nameserver_SignedIn(object sender, EventArgs e)
		{
			Owner.Status = st;
            var connected = new Connected { user = this.user };
            plugin.SendMessage(WrapperMessage.Type.TYPE_CONNECTED, connected);
		}
	}
}

