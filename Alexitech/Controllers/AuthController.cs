using Alexitech.Model;
using Alexitech.Models;
using HarmonyHub;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Alexitech.Controllers
{
    public class AuthController : Controller
    {
        public ActionResult Index(string client_id, string response_type, string state)
        {
            if (client_id != "alexa-skill")
                throw new HttpException(500, "Invalid client id");

            if (response_type != "token")
                throw new HttpException(500, "Invalid response type");

            if (string.IsNullOrEmpty(state))
                throw new HttpException(500, "Invalid state");

            var model = new AuthModels.Index()
            {
                State = state
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Index(AuthModels.Index model)
        {
            if (ModelState.IsValid)
            {
                var model2 = new AuthModels.Network
                {
                    State = model.State,
                    Hostname = Request.UserHostAddress
                };

                return View("Network", model2);
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Network(AuthModels.Network model)
        {
            if (ModelState.IsValid)
            {
                bool success = false;
                try
                {
                    var conn = new HarmonyClientConnection(model.Hostname, 5222);
                    var manualEvent = new ManualResetEvent(false);
                    conn.ClientSocket.OnConnect += (obj) =>
                    {
                        success = true;
                        manualEvent.Set();
                    };
                    conn.OnSocketError += (obj, ex) =>
                    {
                        manualEvent.Set();
                    };
                    try
                    {
                        conn.Open();
                        manualEvent.WaitOne(5000);
                    }
                    finally
                    {
                        try { conn.Close(); } catch { }
                    }
                }
                catch { }

                if (!success)
                {
                    ModelState.AddModelError("Hostname", "Your Harmony Hub could not be reached");
                }

                if (ModelState.IsValid)
                {
                    var model2 = new AuthModels.Login
                    {
                        State = model.State,
                        Hostname = Request.UserHostAddress
                    };

                    return View("Login", model2);
                }
            }

            return View(model);
        }

        private void ClientSocket_OnConnect(object sender)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public ActionResult Login(AuthModels.Login model)
        {
            if (ModelState.IsValid)
            {
                string token = null;
                try
                {
                    token = HarmonyLogin.GetUserAuthToken(model.Username, model.Password);
                }
                catch { }

                if (string.IsNullOrEmpty(token))
                {
                    ModelState.AddModelError("Username", "Invalid username or password");
                }

                if (ModelState.IsValid)
                {
                    using (var manager = new Manager())
                    {
                        var user = manager.Users
                            .FirstOrDefault(o => o.HarmonyUsername == model.Username);

                        if (user == null)
                        {
                            user = new User()
                            {
                                HarmonyUsername = model.Username
                            };
                        }

                        user.HarmonyPassword = "";
                        user.HarmonyToken = token;
                        user.AlexaToken = RandomString(50);
                        user.AlexaUserID = "";
                        user.Hostname = model.Hostname;

                        manager.SaveChanges();

                        string url = ConfigurationManager.AppSettings["AuthUrl"]
                            + "#state=" + Url.Encode(model.State)
                            + "&access_token=" + Url.Encode(user.AlexaToken)
                            + "&token_type=Bearer";

                        return Redirect(url);
                    }
                }
            }

            return View(model);
        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}