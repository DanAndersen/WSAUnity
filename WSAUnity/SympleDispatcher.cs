using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WSAUnity
{
    public class SympleDispatcher
    {
        Dictionary<string, List<Action<object>>> listeners;

        public SympleDispatcher()
        {
            listeners = new Dictionary<string, List<Action<object>>>();
        }

        public void on(string eventLabel, Action<object> fn)
        {
            if (!listeners.ContainsKey(eventLabel))
            {
                listeners[eventLabel] = new List<Action<object>>();
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
