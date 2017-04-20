using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SympleEngineOptions
    {
        public string id;
        public string name;
        public string formats;
        public int preference;
        public Func<bool> support;
    }
}
