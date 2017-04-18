using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WSAUnity
{
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
}
