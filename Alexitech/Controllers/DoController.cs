using Alexitech.Model;
using Alexitech.Scoping;
using HarmonyHub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace Alexitech.Controllers
{
    public class DoController : Controller
    {
        private static object _lock = new object();

        public ActionResult Index(AlexaRoot root)
        {
            Stream req = Request.InputStream;
            req.Seek(0, System.IO.SeekOrigin.Begin);
            string requestBody = new StreamReader(req).ReadToEnd();

            //var input = new JavaScriptSerializer().DeserializeObject(json);

            bool success = false;
            string speech = null;
            bool inSequence = false;
            string intentName = null;
            int? userID = null;

            if (root != null)
            {
                User user = null;

                var session = root.Session;
                if (session != null)
                {
                    if (session.Attributes != null)
                    {
                        string v;
                        if (session.Attributes.TryGetValue("inSequence", out v) && v == "true")
                            inSequence = true;
                    }

                    if (session.User != null && session.User.AccessToken != null)
                    {
                        using (var manager = new Manager())
                        {
                            user = manager.Users
                                .FirstOrDefault(o => o.AlexaToken == session.User.AccessToken);

                            if (user != null && session.User.UserID != null && user.AlexaUserID != session.User.UserID)
                            {
                                user.AlexaUserID = session.User.UserID;

                                manager.SaveChanges();
                            }
                        }
                    }
                }

                var request = root.Request;
                if (request != null)
                {
                    var intent = request.Intent;
                    if (intent != null)
                    {
                        if (intent.Name != null)
                        {
                            intentName = intent.Name;

                            if (user != null)
                            {
                                userID = user.ID;

                                switch (intentName)
                                {
                                    case "AMAZON.HelpIntent":
                                        success = true;
                                        speech = "Here are some sample phrases. Tell the remote to start the TV activity. Or. Tell the remote to pause. Or. Tell the remote to press the mute button.";
                                        break;

                                    case "SequenceStartIntent":
                                        inSequence = true;
                                        success = true;
                                        speech = "OK, I'm listening";
                                        break;

                                    case "SequenceEndIntent":
                                        inSequence = false;
                                        success = true;
                                        speech = "OK, done listening";
                                        break;

                                    default:
                                        {
                                            var values = (intent.Slots ?? new Dictionary<string, AlexaSlot>())
                                                .ToDictionary(o => o.Key, o => o.Value.Value);

                                            success = Command(user, intent.Name, values, out speech);
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                speech = "You need to link your Harmony account using the Alexa app";
                            }
                        }
                        else
                        {
                            speech = "Error. Missing intent name.";
                        }
                    }
                    else
                    {
                        speech = "Error. Missing intent.";
                    }
                }
                else
                {
                    speech = "Error. Missing request.";
                }
            }
            else
            {
                speech = "Error. Missing root.";
            }

            using (Manager m = new Manager())
            {
                var log = new RequestLog()
                {
                    UserID = userID,
                    IntentName = intentName,
                    RequestBody = requestBody,
                    RequestDate = DateTime.Now
                };
                m.RequestLogs.Add(log);

                m.SaveChanges();
            }

            var response = new
            {
                version = "1.0",
                sessionAttributes = new
                {
                    inSequence = (inSequence ? "true" : "false")
                },
                response = new
                {
                    outputSpeech = new
                    {
                        type = "PlainText",
                        text = inSequence 
                            ? (success ? "Yep" : "Hmm")
                            : (speech ?? "OK")
                    },
                    shouldEndSession = !inSequence
                }
            };

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        private string LoginToLogitech(string userAuthToken, string ipAddress, int harmonyPort)
        {
            var authentication = new HarmonyAuthenticationClient(ipAddress, harmonyPort);

            string sessionToken = authentication.SwapAuthToken(userAuthToken);
            if (string.IsNullOrEmpty(sessionToken))
            {
                throw new Exception("Could not swap token on Harmony Hub.");
            }

            return sessionToken;
        }

        private bool Command(User user, string name, Dictionary<string, string> values, out string speech)
        {
            bool success = false;
            speech = null;

            lock (_lock)
            {
                var client = ScopingModule.Application.Ensure<HarmonyClient>(user.ID.ToString(), () =>
                {
                    string sessionToken = LoginToLogitech(user.HarmonyToken, user.Hostname, 5222);
                    var r = new HarmonyClient(user.Hostname, 5222, sessionToken);

                    r.GetConfig();

                    return r;
                });

                var config = new JavaScriptSerializer().Deserialize<HarmonyConfigResult>(client.Config);
                string s;

                // Shortcuts
                switch (name)
                {
                    case "PlayIntent":
                        name = "ButtonIntent";
                        values["Button"] = "play";
                        speech = "Playing";
                        break;

                    case "PauseIntent":
                        name = "ButtonIntent";
                        values["Button"] = "pause";
                        speech = "Pausing";
                        break;
                }

                // Commands
                switch (name)
                {
                    case "ButtonIntent":
                        {
                            string buttonName = (values.TryGetValue("Button", out s) ? s : "Pause").ToLower();

                            if (config != null && config.activity != null)
                            {
                                client.GetCurrentActivity();

                                if (client.CurrentActivity != null)
                                {
                                    var activity = config.activity
                                        .FirstOrDefault(o => o.id == client.CurrentActivity);

                                    if (activity != null)
                                    {
                                        var buttons = activity.controlGroup
                                            .Where(o => o.function != null)
                                            .SelectMany(o => o.function)
                                            .Where(o => o.action != null)
                                            .ToList();

                                        var button = buttons
                                            .OrderBy(o => {
                                                var label = (o.label ?? "")
                                                    .ToLower()
                                                    .Replace("direction ", "");

                                                return Distance(buttonName, label);
                                            })
                                            .FirstOrDefault();

                                        if (button != null)
                                        {
                                            var action = new JavaScriptSerializer().Deserialize<HarmonyAction>(button.action);
                                            if (action != null)
                                            {
                                                client.PressButton(action.deviceId, action.command);
                                                speech = speech ?? "Pressing the " + button.label + " button";
                                                success = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case "ActivityIntent":
                        {
                            string activityName = (values.TryGetValue("Activity", out s) ? s : "TV").ToLower();

                            if (config != null && config.activity != null)
                            {
                                var activity = config.activity
                                    .Where(o => o.id != "-1")
                                    .OrderBy(o => Distance(activityName, (o.label ?? "").ToLower()))
                                    .FirstOrDefault();

                                if (activity != null)
                                {
                                    client.StartActivity(activity.id);
                                    speech = "Starting the " + activity.label + " activity";
                                    success = true;
                                }
                            }
                        }
                        break;

                    case "OffIntent":
                        client.TurnOff();
                        speech = "Powering off";
                        break;
                }
            }

            return success;
        }

        public static int Distance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1 /* delete */, d[i, j - 1] + 1 /* insert */),
                        d[i - 1, j - 1] + cost /* substitute */);
                }
            }
            // Step 7
            return d[n, m];
        }

        public class AlexaRoot
        {
            public string Version { get; set; }
            public AlexaSession Session { get; set; }
            public AlexaRequest Request { get; set; }
        }

        public class AlexaSession
        {
            public bool New { get; set; }
            public string SessionID { get; set; }
            public AlexaUser User { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
        }

        public class AlexaUser
        {
            public string UserID { get; set; }
            public string AccessToken { get; set; }
        }

        public class AlexaRequest
        {
            public string Type { get; set; }
            public string RequestID { get; set; }
            public string Timestamp { get; set; }
            public AlexaIntent Intent { get; set; }
        }

        public class AlexaIntent
        {
            public string Name { get; set; }
            public Dictionary<string, AlexaSlot> Slots { get; set; }
        }

        public class AlexaSlot
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}