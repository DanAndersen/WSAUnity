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
        public override void add(object peer)
        {
            Debug.WriteLine("symple:roster: adding " + peer);
            if (peer == null || peer.id == null || peer.user == null)
            {
                throw new Exception("cannot add invalid peer");
            }

            base.add(peer);
            this.client.dispatch("addPeer", peer);
        }

        // remove the peer matching an ID or address string: user|id
        public override object remove(string id)
        {
            id = SympleParseAddress(id).id ?? id;
            var peer = base.remove(id);
            Debug.WriteLine("symple:roster: removing " + id + ", " + peer);
            if (peer != null)
            {
                this.client.dispatch("removePeer", peer);
            }
            return peer;
        }

        // get the peer matching an ID or address string: user|id
        public override object get(string id)
        {
            // handle IDs
            var peer = base.get(id);
            if (peer != null)
            {
                return peer;
            }

            // handle address strings
            return this.findOne(SympleParseAddress(id));
        }

        public void update(var data)
        {
            if (data == null || data.id == null)
            {
                return;
            }

            var peer = this.get(data.id);
            if (peer != null)
            {
                foreach (var key in data)
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
