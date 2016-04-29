using System;
using System.Runtime.Remoting.Messaging;

namespace Alexitech.Scoping
{
    public class SimpleScope : IScope
    {
        private ScopingData _data;

        public SimpleScope()
        {
            _data = new ScopingData();
        }

        public ScopingData Data
        {
            get { return _data; }
        }
    }
}