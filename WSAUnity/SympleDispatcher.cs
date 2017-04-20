using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
#endif
using System.Diagnostics;

namespace WSAUnity
{
    public class SympleDispatcher
    {
        public Dictionary<string, List<object>> listeners { get; }

        public SympleDispatcher()
        {
            listeners = new Dictionary<string, List<object>>();
        }

        public void on(string eventLabel, Action<object> fn)
        {
            if (!listeners.ContainsKey(eventLabel))
            {
                listeners[eventLabel] = new List<object>();
            }
            listeners[eventLabel].Add(fn);
        }

        public void clear(string eventLabel, Action<object> fn)
        {
            if (listeners.ContainsKey(eventLabel))
            {
                listeners[eventLabel].Remove(fn);
            }
        }

        public void dispatch(string eventLabel, object arg)
        {
            if (listeners.ContainsKey(eventLabel))
            {
                foreach (Action<object> fn in listeners[eventLabel])
                {
                    fn(arg);
                }
            }
        }
    }
}
