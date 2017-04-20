using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
#endif


namespace WSAUnity
{
#if NETFX_CORE
    class JObjectWithActions : JObject
    {
        public Dictionary<string, Action<object>> actions;

        public JObjectWithActions() : base()
        {
            actions = new Dictionary<string, Action<object>>();
        }
    }
#endif
}
