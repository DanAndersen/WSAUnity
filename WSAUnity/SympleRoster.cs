using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
#endif
using System.Diagnostics;


namespace WSAUnity
{
    public class SympleRoster : SympleManager
    {
        SympleClient client;

        public SympleRoster(SympleClient client) : base()
        {
            this.client = client;
        }

        // add a peer object to the roster
#if NETFX_CORE
        public override void add(JObject peer)
        {
            if (peer == null || peer["id"] == null || peer["user"] == null)
            {
                throw new Exception("cannot add invalid peer");
            }

            base.add(peer);
            this.client.dispatch("addPeer", peer);
        }
#endif

#if NETFX_CORE
        // remove the peer matching an ID or address string: user|id
        public override JObject remove(string id)
        {
            var addr = Symple.parseAddress(id);

            id = (string)addr["id"] ?? id;
            var peer = base.remove(id);
            if (peer != null)
            {
                this.client.dispatch("removePeer", peer);
            }
            return peer;
        }
#endif

#if NETFX_CORE
        // get the peer matching an ID or address string: user|id
        public override JObject get(string id)
        {
            // handle IDs
            JObject peer = base.get(id);
            if (peer != null)
            {
                return peer;
            }

            // handle address strings
            return this.findOne(Symple.parseAddress(id));
        }
#endif

#if NETFX_CORE
        public void update(JObject data)
        {
            if (data == null || data["id"] == null)
            {
                return;
            }

            JObject peer = this.get((string)data["id"]);
            if (peer != null)
            {
                foreach (var prop in data.Properties())
                {
                    peer[prop.Name] = data[prop.Name];
                }
            } else
            {
                this.add(data);
            }
        }
#endif
    }
}
