using System.Diagnostics;
using System.IO;
using ProtoBuf;
using networkplugin_csharp;
using pbnetwork;

namespace networkplugin_demo
{
    public class DumbPlugin : NetworkPlugin
    {
        private void HandleLogin(object sender, LogInEventArgs login)
        {

            Trace.WriteLine(string.Format("Login: {0}, {1}, {2}", login.LoginPayload.user, login.LoginPayload.password, login.LoginPayload.legacyName));

            var connected = new Connected { user = login.LoginPayload.user };
            SendMessage(WrapperMessage.Type.TYPE_CONNECTED, connected);
        }

        private void HandleLogout(object sender, LogOutEventArgs logout)
        {
            Trace.WriteLine(string.Format("Logout: {0}", logout.LogoutPayload.user));
        }
        public DumbPlugin(string host, string port) : base(host, port)
        {
            LoggedIn += HandleLogin;
            LoggedOut += HandleLogout;
        }
    
    }
}
