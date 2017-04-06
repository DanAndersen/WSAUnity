using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SympleClient : SympleDispatcher
    {
        private Socket socket;
        SympleRoster roster;

        SympleClientOptions options;

        IO.Options ioOptions;

        public SympleClient(SympleClientOptions options) : base()
        {
            this.options = options;
            options.url = options.url ?? "http://localhost:4000";
            options.secure = (options.url != null && (options.url.IndexOf("https") == 0 || options.url.IndexOf("wss") == 0));
            options.token = null;
            
            // TODO: set up the "options"
            throw new NotImplementedException();

            this.peer = options.peer ?? { };
            this.peer.rooms = options.peer.rooms ?? [];
            this.roster = new SympleRoster(this);
            this.socket = null;

            ioOptions = new IO.Options();
            ioOptions.Secure = options.secure;
            ioOptions.Port = serverSSLPort;
            ioOptions.Hostname = serverHostname;
            ioOptions.IgnoreServerCertificateValidation = true;
        }

        public void connect()
        {
            Debug.WriteLine("symple:client: connecting");

            if (this.socket != null)
            {
                throw new Exception("the client socket is not null");
            }

            this.socket = IO.Socket(this.options.url, this.ioOptions);
            this.socket.On(Socket.EVENT_CONNECT, () => {
                Debug.WriteLine("ssymple:client: connected");

                Dictionary<string, object> announceData = new Dictionary<string, object>();
                announceData["user"] = options.user ?? "";
                announceData["name"] = options.name ?? "";
                announceData["type"] = options.type ?? "";
                announceData["token"] = options.token ?? "";

                string announceDataJsonString = JsonConvert.SerializeObject(announceData, Formatting.None);

                this.socket.Emit("announce", (res) => {
                    Debug.WriteLine("symple:client: announced " + res);
                    if (res.status != 200)
                    {
                        this.setError("auth", res);
                        return;
                    }
                    this.peer = SympleExtend(this.peer, res.data);
                    this.roster.add(res.data);
                    this.sendPresence(probe: true);
                    this.dispatch("announce", res);
                    this.socket.On(Socket.EVENT_MESSAGE, (m) =>
                    {
                        Debug.WriteLine("symple:client receive " + m);

                        throw new NotImplementedException();
                    });


                },  announceDataJsonString);
            });

            this.socket.On(Socket.EVENT_ERROR, () =>
            {
                // this is triggered when any transport fails, so not necessarily fatal
                this.dispatch("connect");
            });

            this.socket.On("connecting", () =>
            {
                Debug.WriteLine("symple:client: connecting");
                this.dispatch("connecting");
            });

            this.socket.On(Socket.EVENT_RECONNECTING, () =>
            {
                Debug.WriteLine("symple:client: reconnecting");
                this.dispatch("reconnecting");
            });

            this.socket.On("connect_failed", () =>
            {
                // called when all transports fail
                Debug.WriteLine("symple:client: connect failed");
                this.dispatch("connect_failed");
                this.setError("connect");
            });

            this.socket.On(Socket.EVENT_DISCONNECT, () =>
            {
                Debug.WriteLine("symple:client: disconnect");
                this.peer.online = false;
                this.dispatch("disconnect");
            });
        }

        // disconnect from the server
        public void disconnect()
        {
            if (this.socket != null)
            {
                this.socket.Disconnect();
            }
        }

        // return the online status
        public bool online()
        {
            return this.peer.online;
        }

        public void join(var room)
        {
            throw new NotImplementedException();
        }

        public void leave(var room)
        {
            throw new NotImplementedException();
        }

        public void send(var m, var to)
        {
            throw new NotImplementedException();
        }

        public void respond(var m)
        {
            this.send(m, m.from);
        }

        public void sendMessage(var m, var to)
        {
            this.send(m, to);
        }


    }
}
