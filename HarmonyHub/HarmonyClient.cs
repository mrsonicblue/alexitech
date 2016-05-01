using agsXMPP;
using agsXMPP.protocol.client;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace HarmonyHub
{
    /// <summary>
    /// Client to interrogate and control Logitech Harmony Hub.
    /// </summary>
    public class HarmonyClient
    {
        private static Regex IdentityRegex = new Regex("\">(.*)</oa>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        protected ManualResetEvent Wait = new ManualResetEvent(false);

        protected HarmonyClientConnection Xmpp;

        private string _username;
        private string _token;

        public string RawConfig { get; set; }
        public HarmonyConfigResult Config { get; set; }
        public string CurrentActivity { get; set; }

        /// <summary>
        /// Constructor with standard settings for a new HarmonyClient
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="token"></param>
        public HarmonyClient(string ipAddress, int port, string token)
        {
            Xmpp = new HarmonyClientConnection(ipAddress, port);
            Xmpp.OnLogin += delegate { Wait.Set(); };

            _username = string.Format("{0}@x.com", token);
            _token = token;
        }

        protected void EnsureConnection()
        {
            if (Xmpp.XmppConnectionState == XmppConnectionState.Disconnected)
            {
                Xmpp.Open(_username, _token);

                try
                {
                    Wait.WaitOne(5000);
                }
                finally { Wait.Reset(); }
            }
        }

        #region Send Messages to HarmonyHub

        /// <summary>
        /// Send message to HarmonyHub to request Configuration.
        /// Result is parsed by OnIq based on ClientCommandType
        /// </summary>
        public void GetConfig()
        {
            EnsureConnection();

            var iqToSend = new IQ { Type = IqType.get, Namespace = "", From = "1", To = "guest" };
            iqToSend.AddChild(HarmonyDocuments.ConfigDocument());
            iqToSend.GenerateId();

            var iqGrabber = new IqGrabber(Xmpp);
            var iq = iqGrabber.SendIq(iqToSend, 10000);

            if (iq != null)
            {
                var match = IdentityRegex.Match(iq.InnerXml);
                if (match.Success)
                {
                    RawConfig = match.Groups[1].ToString();
                    Config = null;
                    try
                    {
                        Config = new JavaScriptSerializer().Deserialize<HarmonyConfigResult>(RawConfig);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Send message to HarmonyHub to start a given activity
        /// Result is parsed by OnIq based on ClientCommandType
        /// </summary>
        /// <param name="activityId"></param>
        public void StartActivity(string activityId)
        {
            EnsureConnection();

            var iqToSend = new IQ { Type = IqType.get, Namespace = "", From = "1", To = "guest" };
            iqToSend.AddChild(HarmonyDocuments.StartActivityDocument(activityId));
            iqToSend.GenerateId();

            Xmpp.Send(iqToSend);
        }

        /// <summary>
        /// Send message to HarmonyHub to request current activity
        /// Result is parsed by OnIq based on ClientCommandType
        /// </summary>
        public void GetCurrentActivity()
        {
            EnsureConnection();

            var iqToSend = new IQ { Type = IqType.get, Namespace = "", From = "1", To = "guest" };
            iqToSend.AddChild(HarmonyDocuments.GetCurrentActivityDocument());
            iqToSend.GenerateId();

            var iqGrabber = new IqGrabber(Xmpp);
            var iq = iqGrabber.SendIq(iqToSend, 10);

            if (iq != null)
            {
                var match = IdentityRegex.Match(iq.InnerXml);
                if (match.Success)
                {
                    CurrentActivity = match.Groups[1].ToString().Split('=')[1];
                }
            }
        }

        /// <summary>
        /// Send message to HarmonyHub to request to press a button
        /// Result is parsed by OnIq based on ClientCommandType
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="command"></param>
        public void PressButton(string deviceId, string command)
        {
            EnsureConnection();

            var iqToSend = new IQ { Type = IqType.get, Namespace = "", From = "1", To = "guest" };
            iqToSend.AddChild(HarmonyDocuments.IRCommandDocument(deviceId, command));
            iqToSend.GenerateId();

            Xmpp.Send(iqToSend);
        }

        /// <summary>
        /// Send message to HarmonyHub to request to run a sequence
        /// Result is parsed by OnIq based on ClientCommandType
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="command"></param>
        public void Sequence(int sequenceId)
        {
            EnsureConnection();

            var iqToSend = new IQ { Type = IqType.get, Namespace = "", From = "1", To = "guest" };
            iqToSend.AddChild(HarmonyDocuments.SequenceDocument(sequenceId));
            iqToSend.GenerateId();

            Xmpp.Send(iqToSend);
        }

        /// <summary>
        /// Send message to HarmonyHub to request to turn off all devices
        /// </summary>
        public void TurnOff()
        {
            //GetCurrentActivity();
            //if (CurrentActivity != "-1")
            //{
                StartActivity("-1");
            //}
        }

        #endregion
    }
}
