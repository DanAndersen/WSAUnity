using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
#endif
using System.Diagnostics;

namespace WSAUnity
{
    public class SympleManager
    {
#if NETFX_CORE
        List<JObject> store;
#endif
        string key;

        public SympleManager()
        {
            // NOTE: removed all references to this.options and this.key since they appear to be unused
            this.key = "id";
#if NETFX_CORE
            this.store = new List<JObject>();
#endif
        }

#if NETFX_CORE
        public virtual void add(JObject value)
        {
            this.store.Add(value);
        }
#endif

#if NETFX_CORE
        public virtual JObject remove(string key)
        {
            JObject res = null;
            for (int i = 0; i < this.store.Count; i++)
            {
                if ((string)(this.store[i][this.key]) == key)
                {
                    res = this.store[i];
                    this.store.RemoveAt(i);
                    break;
                }
            }
            return res;
        }
#endif

#if NETFX_CORE
        public virtual JObject get(string key)
        {
            for (int i = 0; i < this.store.Count; i++)
            {
                if ((string)(this.store[i][this.key]) == key)
                {
                    return this.store[i];
                }
            }
            return null;
        }
#endif

#if NETFX_CORE
        public List<JObject> find(JObject parameters)
        {
            List<JObject> res = new List<JObject>();
            for (int i = 0; i < this.store.Count; i++)
            {
                if (Symple.match(parameters, this.store[i]))
                {
                    res.Add(this.store[i]);
                }
            }
            return res;
        }
#endif

#if NETFX_CORE
        public JObject findOne(JObject parameters)
        {
            var res = this.find(parameters);
            return (res.Count > 0) ? res[0] : null;
        }
#endif

#if NETFX_CORE
        public JObject last()
        {
            return this.store.Last();
        }
#endif

#if NETFX_CORE
        public int size()
        {
            return this.store.Count;
        }
#endif
    }
}
