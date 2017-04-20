using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
#endif


namespace WSAUnity
{
#if NETFX_CORE
    public class SympleMessage : JObject
    {
        public SympleMessage(JObject json)
        {
            foreach (var prop in json.Properties())
            {
                this[prop.Name] = json[prop.Name];
            }

            this["type"] = "message";
        }
    }
#endif
}
