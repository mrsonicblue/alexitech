using System;
using System.Collections.Generic;

namespace Alexitech.Scoping
{
    public class ScopingData : IDisposable
    {
        private object _lock = new object();
        private Dictionary<Type, Dictionary<string, object>> _data = new Dictionary<Type, Dictionary<string,object>>();

        private void CheckDisposed()
        {
            if (_data == null)
                throw new Exception("Data has been disposed");
        }

        public bool TryGet<T>(out T value)
        {
            return TryGet<T>("", out value);
        }

        public bool TryGet<T>(string key, out T value)
        {
            lock (_lock)
            {
                CheckDisposed();

                Dictionary<string, object> data;
                if (_data.TryGetValue(typeof(T), out data))
                {
                    object o;
                    if (data.TryGetValue(key, out o))
                    {
                        value = (T)o;
                        return true;
                    }
                }

                value = default(T);
                return false;
            }
        }

        public T Get<T>()
        {
            return Get<T>("");
        }

        public T Get<T>(string key)
        {
            lock (_lock)
            {
                CheckDisposed();

                Dictionary<string, object> data;
                if (_data.TryGetValue(typeof(T), out data))
                {
                    object o;
                    if (data.TryGetValue(key, out o))
                        return (T)o;
                }

                return default(T);
            }
        }

        public void Set<T>(T value)
        {
            Set<T>("", value);
        }

        public void Set<T>(string key, T value)
        {
            lock (_lock)
            {
                CheckDisposed();

                Dictionary<string, object> data;
                if (!_data.TryGetValue(typeof(T), out data))
                {
                    data = new Dictionary<string, object>();
                    _data[typeof(T)] = data;
                }

                data[key] = value;
            }
        }

        public T Ensure<T>()
        {
            return Ensure<T>("");
        }

        public T Ensure<T>(string key)
        {
            lock (_lock)
            {
                T value;
                if (!TryGet<T>(key, out value))
                {
                    value = Activator.CreateInstance<T>();
                    Set<T>(value);
                }

                return value;
            }
        }

        public T Ensure<T>(Func<T> getter)
        {
            return Ensure<T>("", getter);
        }

        public T Ensure<T>(string key, Func<T> getter)
        {
            lock (_lock)
            {
                T value;
                if (!TryGet<T>(key, out value) || value == null)
                {
                    value = getter();
                    Set<T>(key, value);
                }

                return value;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_data != null)
                {
                    foreach (Dictionary<string, object> data in _data.Values)
                    {
                        foreach (object o in data.Values)
                        {
                            IDisposable d = o as IDisposable;
                            if (d != null)
                                d.Dispose();
                        }

                        data.Clear();
                    }

                    _data.Clear();
                    _data = null;
                }
            }
        }
    }
}