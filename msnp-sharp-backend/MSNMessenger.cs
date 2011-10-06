using System;
using System.Timers;
using System.Collections;
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
		private System.Threading.Timer timer;
		private Queue DisplayImageQueue;

		public MSNMessenger(MSNPlugin plugin, string user, string legacyName, string password)
		{
			Console.WriteLine("AAAAAAA2");
			this.plugin = plugin;
			this.user = user;
			timer = null;
			DisplayImageQueue = new Queue();

			
			Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);
			Nameserver.AuthenticationError += new EventHandler<ExceptionEventArgs>(Nameserver_AuthenticationError);
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
		
		private void Nameserver_AuthenticationError(object sender, ExceptionEventArgs e) {
			var message = new Disconnected {user = this.user, error = 0, message = e.Exception.InnerException.Message};
			plugin.SendMessage(WrapperMessage.Type.TYPE_DISCONNECTED, message);
			plugin.messengers.Remove(user);
			plugin = null;
			if (timer != null) {
				timer.Dispose();
			}
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
			if (e.NewStatus != PresenceStatus.Offline) {
				DisplayImageQueue.Enqueue(e.Contact);
			}
			ContactChanged(e.Contact);
		}

        private void RequestDisplayImage(Contact remoteContact, DisplayImage updateImage)
        {
            if (remoteContact.Status != PresenceStatus.Offline)
            {
                if (remoteContact.DisplayImage != remoteContact.UserTileLocation)
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
			}
			
			if (remoteContact.UserTileURL != null && remoteContact.ClientType != IMAddressInfoType.WindowsLive) {
				HttpAsyncDataDownloader.BeginDownload(remoteContact.UserTileURL.AbsoluteUri, new EventHandler<ObjectEventArgs>(DownloadedDisplayImage), null);
			}

        }
		
		private void FetchNextDisplayImage(object data) {
			if (DisplayImageQueue.Count == 0)
				return;
			Contact c = (Contact) DisplayImageQueue.Dequeue();
			Console.WriteLine("Fetching DisplayImage of" + c.Account);
			RequestDisplayImage(c, null);
		}

		private void Nameserver_SignedIn(object sender, EventArgs e)
		{
			Owner.Status = st;
			timer = new System.Threading.Timer(FetchNextDisplayImage, "", 3000, 5000);
            var connected = new Connected { user = this.user };
            plugin.SendMessage(WrapperMessage.Type.TYPE_CONNECTED, connected);
		}
	}
}

