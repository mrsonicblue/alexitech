using agsXMPP;
using agsXMPP.protocol.client;
using System.Text.RegularExpressions;

namespace HarmonyHub
{
    /// <summary>
    /// HarmonyClient for swapping a UserAuthToken for a Session Token
    /// </summary>
    public class HarmonyAuthenticationClient : HarmonyClient
    {
        private static Regex IdentityRegex = new Regex("identity=([A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12}):status", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public HarmonyAuthenticationClient(string ipAddress, int port)
            : base(ipAddress, port, "guest")
        {
        }

        /// <summary>
        /// Send message to HarmonyHub with UserAuthToken, wait for SessionToken
        /// </summary>
        /// <param name="userAuthToken"></param>
        /// <returns></returns>
        public string SwapAuthToken(string userAuthToken)
        {
            EnsureConnection();

            var iqToSend = new IQ { Type = IqType.get, Namespace = "", From = "1", To = "guest" };
            iqToSend.AddChild(HarmonyDocuments.LogitechPairDocument(userAuthToken));
            iqToSend.GenerateId();

            var iqGrabber = new IqGrabber(Xmpp);
            var iq = iqGrabber.SendIq(iqToSend, 5000);

            if (iq != null)
            {
                var match = IdentityRegex.Match(iq.InnerXml);
                if (match.Success)
                {
                    return match.Groups[1].ToString();
                }
            }

            return null;
        }
    }
}
