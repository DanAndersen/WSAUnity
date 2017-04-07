using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSAUnity
{
    public class SympleManager
    {
        List<Dictionary<string, object>> store;
        string key;

        public SympleManager(object options)
        {
            // NOTE: removed all references to this.options and this.key since they appear to be unused
            this.key = "id";
            this.store = new List<Dictionary<string, object>>();
        }

        public virtual void add(Dictionary<string, object> value)
        {
            this.store.Add(value);
        }

        public virtual Dictionary<string, object> remove(string key)
        {
            Dictionary<string, object> res = null;
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

        public virtual Dictionary<string, object> get(string key)
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

        public List<Dictionary<string, object>> find(Dictionary<string, object> parameters)
        {
            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            for (int i = 0; i < this.store.Count; i++)
            {
                if (Symple.match(parameters, this.store[i]))
                {
                    res.Add(this.store[i]);
                }
            }
            return res;
        }

        public Dictionary<string, object> findOne(Dictionary<string, object> parameters)
        {
            var res = this.find(parameters);
            return (res.Count > 0) ? res[0] : null;
        }

        public Dictionary<string, object> last()
        {
            return this.store.Last();
        }

        public int size()
        {
            return this.store.Count;
        }
    }
}
