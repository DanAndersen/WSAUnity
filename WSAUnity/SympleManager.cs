using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSAUnity
{
    public class SympleManager
    {
        List<object> store;
        string key;

        public SympleManager(object options)
        {
            this.options = options ?? {};
            this.key = this.options.key ?? "id";
            this.store = new List<object>();
        }

        public virtual void add(object value)
        {
            this.store.Add(value);
        }

        public virtual object remove(string key)
        {
            object res = null;
            for (int i = 0; i < this.store.Count; i++)
            {
                if (this.store[i][this.key] == key)
                {
                    res = this.store[i];
                    this.store.RemoveAt(i);
                    break;
                }
            }
            return res;
        }

        public virtual object get(string key)
        {
            for (int i = 0; i < this.store.Count; i++)
            {
                if (this.store[i][this.key] == key)
                {
                    return this.store[i];
                }
            }
            return null;
        }

        public List<object> find(var parameters)
        {
            List<object> res = new List<object>();
            for (int i = 0; i < this.store.Count; i++)
            {
                if (SympleMatch(parameters, this.store[i]))
                {
                    res.Add(this.store[i]);
                }
            }
            return res;
        }

        public object findOne(var parameters)
        {
            var res = this.find(parameters);
            return (res.Count > 0) ? res[0] : null;
        }

        public object last()
        {
            return this.store.Last();
        }

        public int size()
        {
            return this.store.Count;
        }
    }
}
