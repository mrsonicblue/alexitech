using System;
using System.Runtime.Remoting.Messaging;

namespace Alexitech.Scoping
{
    public class CallContextScope : IScope
    {
        private const string CONTEXTKEY = "CallContextScope";

        private static object _lock = new object();

        private ScopingData _data;

        public CallContextScope()
        {
            lock (_lock)
            {
                _data = CallContext.GetData(CONTEXTKEY) as ScopingData;
                if (_data == null)
                {
                    _data = new ScopingData();
                    CallContext.SetData(CONTEXTKEY, _data);
                }
            }
        }

        public ScopingData Data
        {
            get { return _data; }
        }
    }
}