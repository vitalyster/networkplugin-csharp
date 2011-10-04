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
		private string user;
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
			ContactService.ContactAdded += new EventHandler<ListMutateEventArgs>(ContactService_ContactAdded);
			Nameserver.AutoSynchronize = true;
			
			Credentials = new Credentials(legacyName, password);
			Connect();
		}
		
        private void Nameserver_TextMessageReceived(object sender, TextMessageArrivedEventArgs e)
        {
			var message = new ConversationMessage {userName = this.user, buddyName = e.Sender.Account, message = e.TextMessage.Text};
			plugin.SendMessage(WrapperMessage.Type.TYPE_CONV_MESSAGE, message);
        }

        private void Nameserver_TypingMessageReceived(object sender, TypingArrivedEventArgs e)
        {
        	
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
		
		private void Nameserver_ContactOnline(object sender, ContactStatusChangedEventArgs e)
		{
			var buddy = new Buddy {userName = this.user, buddyName = e.Contact.Account, alias = e.Contact.Name,
			                        groups = e.Contact.ContactGroups.Count == 0 ? "Buddies" : e.Contact.ContactGroups[0].ToString(), status = MSNStatusTypeToPluginType(e.NewStatus) };
			plugin.SendMessage(WrapperMessage.Type.TYPE_BUDDY_CHANGED, buddy);
		}

		private void Nameserver_SignedIn(object sender, EventArgs e)
		{
			Owner.Status = st;
            var connected = new Connected { user = this.user };
            plugin.SendMessage(WrapperMessage.Type.TYPE_CONNECTED, connected);
		}
	}
}

