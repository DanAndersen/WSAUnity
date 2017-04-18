using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSAUnity
{
    public class SympleCommand : Dictionary<string, object>
    {
        public SympleCommand(Dictionary<string, object> json): base()
        {
            this.fromJSON(json);
            this["type"] = "command";
        }
        
        private void fromJSON(Dictionary<string, object> json)
        {
            foreach (string key in json.Keys)
            {
                this[key] = json[key];
            }
        }
    }
}
