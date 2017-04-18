using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WSAUnity
{
    class JObjectWithActions : JObject
    {
        public Dictionary<string, Action<object>> actions;

        public JObjectWithActions() : base()
        {
            actions = new Dictionary<string, Action<object>>();
        }
    }
}
