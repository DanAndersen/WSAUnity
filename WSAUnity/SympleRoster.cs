using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public override void add(Dictionary<string, object> peer)
        {
            Debug.WriteLine("symple:roster: adding " + peer);
            if (peer == null || !peer.ContainsKey("id") || !peer.ContainsKey("user"))
            {
                throw new Exception("cannot add invalid peer");
            }

            base.add(peer);
            this.client.dispatch("addPeer", peer);
        }

        // remove the peer matching an ID or address string: user|id
        public override Dictionary<string, object> remove(string id)
        {
            var addr = Symple.parseAddress(id);

            id = (string)addr["id"] ?? id;
            var peer = base.remove(id);
            Debug.WriteLine("symple:roster: removing " + id + ", " + peer);
            if (peer != null)
            {
                this.client.dispatch("removePeer", peer);
            }
            return peer;
        }

        // get the peer matching an ID or address string: user|id
        public override Dictionary<string, object> get(string id)
        {
            // handle IDs
            Dictionary<string, object> peer = base.get(id);
            if (peer != null)
            {
                return peer;
            }

            // handle address strings
            return this.findOne(Symple.parseAddress(id));
        }

        public void update(Dictionary<string, object> data)
        {
            if (data == null || !data.ContainsKey("id"))
            {
                return;
            }

            Dictionary<string, object> peer = this.get((string)data["id"]);
            if (peer != null)
            {
                foreach (var key in data.Keys)
                {
                    peer[key] = data[key];
                }
            } else
            {
                this.add(data);
            }
        }
    }
}
