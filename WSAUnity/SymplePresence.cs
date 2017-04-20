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
    public class SymplePresence : JObject
    {
        public SymplePresence(JObject json)
        {
            foreach (var prop in json.Properties())
            {
                this[prop.Name] = json[prop.Name];
            }

            this["type"] = "presence";
        }
    }
#endif
}
