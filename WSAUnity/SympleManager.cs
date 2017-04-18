using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WSAUnity
{
    public class SympleManager
    {
        List<JObject> store;
        string key;

        public SympleManager()
        {
            // NOTE: removed all references to this.options and this.key since they appear to be unused
            this.key = "id";
            this.store = new List<JObject>();
        }

        public virtual void add(JObject value)
        {
            this.store.Add(value);
        }

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

        public JObject findOne(JObject parameters)
        {
            var res = this.find(parameters);
            return (res.Count > 0) ? res[0] : null;
        }

        public JObject last()
        {
            return this.store.Last();
        }

        public int size()
        {
            return this.store.Count;
        }
    }
}
