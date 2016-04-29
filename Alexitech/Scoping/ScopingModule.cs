using System;
using System.Web;
using System.Web.SessionState;

namespace Alexitech.Scoping
{
    public class ScopingModule : IHttpModule
    {
        private const string CACHE_KEY = "SCOPINGMODULEDATA";

        private static object _lock;
        private static int _instanceCount;

        private HttpApplication _app = null;

        static ScopingModule()
        {
            _lock = new object();
            _instanceCount = 0;
        }

        public void Init(HttpApplication app)
        {
            lock (_lock)
            {
                //if (_instanceCount == 0)
                //{
                //}

                _instanceCount++;
            }

            _app = app;

            _app.EndRequest += new EventHandler(Context_EndRequest);

            SessionStateModule sessionModule = _app.Modules["Session"] as SessionStateModule;
            if (sessionModule != null)
            {
                sessionModule.End += new EventHandler(Session_End);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _instanceCount--;

                if (_instanceCount == 0)
                {
                    Scope scope = GetApplicationScope(_app.Application, false);
                    if (scope != null)
                        scope.Data.Dispose();
                }
            }

            _app = null;
        }

        void Session_End(object sender, EventArgs e)
        {
            Scope scope = GetSessionScope(false);
            if (scope != null)
                scope.Data.Dispose();
        }

        void Context_EndRequest(object sender, EventArgs e)
        {
            Scope scope = GetRequestScope(false);
            if (scope != null)
                scope.Data.Dispose();
        }

        public static IScope Application
        {
            get { return GetApplicationScope(true); }
        }

        public static IScope Session
        {
            get { return GetSessionScope(true); }
        }

        public static IScope Request
        {
            get { return GetRequestScope(true); }
        }

        private static HttpContext GetContext()
        {
            if (_instanceCount == 0)
                throw new Exception("ScopingModule is not installed");

            HttpContext ctx = HttpContext.Current;
            if (ctx == null)
                throw new Exception("HttpContext unavailable");

            return ctx;
        }

        private static Scope GetApplicationScope(bool ensure)
        {
            HttpContext ctx = GetContext();
            return GetApplicationScope(ctx.Application, ensure);
        }

        private static Scope GetApplicationScope(HttpApplicationState state, bool ensure)
        {
            if (state == null)
                throw new Exception("HttpApplicationState is unavailable");

            lock (state)
            {
                Scope scope = state[CACHE_KEY] as Scope;
                if (scope == null && ensure)
                {
                    scope = new Scope();
                    state[CACHE_KEY] = scope;
                }
                return scope;
            }
        }

        private static Scope GetSessionScope(bool ensure)
        {
            HttpContext ctx = GetContext();
            HttpSessionState state = ctx.Session;
            if (state == null)
                throw new Exception("HttpSessionState is unavailable");

            lock (state)
            {
                Scope scope = state[CACHE_KEY] as Scope;
                if (scope == null && ensure)
                {
                    scope = new Scope();
                    state[CACHE_KEY] = scope;
                }
                return scope;
            }
        }

        private static Scope GetRequestScope(bool ensure)
        {
            HttpContext ctx = GetContext();
            lock (ctx.Items)
            {
                Scope scope = ctx.Items[CACHE_KEY] as Scope;
                if (scope == null && ensure)
                {
                    scope = new Scope();
                    ctx.Items[CACHE_KEY] = scope;
                }
                return scope;
            }
        }

        private class Scope : IScope
        {
            ScopingData _data = new ScopingData();

            public ScopingData Data
            {
                get { return _data; }
            }
        }
    }
}