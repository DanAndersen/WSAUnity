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
        Dictionary<string, object> peer;

        public SympleClient(SympleClientOptions options) : base()
        {
            this.options = options;
            options.url = options.url ?? "http://localhost:4000";
            options.secure = (options.url != null && (options.url.IndexOf("https") == 0 || options.url.IndexOf("wss") == 0));
            options.token = null;

            this.peer = options.peer ?? new Dictionary<string, object>();
            this.peer["rooms"] = options.peer.ContainsKey("rooms") ? options.peer["rooms"] : new List<object>();
            this.roster = new SympleRoster(this);
            this.socket = null;

            Uri socketUri = new Uri(options.url);

            ioOptions = new IO.Options();
            ioOptions.Secure = options.secure;
            ioOptions.Port = socketUri.Port;
            ioOptions.Hostname = socketUri.Host;
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
                announceData["user"] = this.peer["user"] ?? "";
                announceData["name"] = this.peer["name"] ?? "";
                announceData["type"] = this.peer["type"] ?? "";
                announceData["token"] = options.token ?? "";

                string announceDataJsonString = JsonConvert.SerializeObject(announceData, Formatting.None);

                this.socket.Emit("announce", (resObj) => {
                    Debug.WriteLine("symple:client: announced " + resObj);

                    Dictionary<string, object> res = (Dictionary<string, object>)resObj;

                    if ((int)res["status"] != 200)
                    {
                        this.setError("auth", (string)resObj);
                        return;
                    }

                    Dictionary<string, object> resData = (Dictionary < string, object> ) res["data"];

                    this.peer = Symple.extend(this.peer, resData);
                    this.roster.add(resData);

                    Dictionary<string, object> sendPresenceParams = new Dictionary<string, object>();
                    sendPresenceParams["probe"] = true;

                    this.sendPresence(sendPresenceParams);
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
                this.peer["online"] = false;
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
            return (bool) this.peer["online"];
        }

        public void join(Dictionary<string, object> room)
        {
            this.socket.Emit("join", room);
        }

        public void leave(Dictionary<string, object> room)
        {
            this.socket.Emit("leave", room);
        }

        public void sendPresence(Dictionary<string, object> p)
        {
            p = p ?? new Dictionary<string, object>();
            if (p["data"] != null)
            {
                Dictionary<string, object> pDataObj = (Dictionary<string, object>)p["data"];
                p["data"] = Symple.merge(this.peer, pDataObj);
            } else
            {
                p["data"] = this.peer;
            }
            this.send(Symple.initPresence(p));
        }

        // send a message to the given peer
        // m = JSON object
        // to = either a string or a JSON object to build an address from
        public void send(Dictionary<string, object> m, object to = null)
        {
            if (!this.online())
            {
                throw new Exception("cannot send messages while offline"); // TODO: add to pending queue?
            }

            if (m.GetType() != typeof(Dictionary<string, object>))
            {
                throw new Exception("message must be an object");
            }

            if (m["type"].GetType() != typeof(string))
            {
                m["type"] = "message";
            }

            if (m["id"] == null)
            {
                m["id"] = Symple.randomString(8);
            }

            if (to != null)
            {
                m["to"] = to;
            }

            if (m["to"].GetType() == typeof(Dictionary<string, object>))
            {
                Dictionary<string, object> mToObj = (Dictionary<string, object>)m["to"];
                m["to"] = Symple.buildAddress(mToObj);
            }

            if (m["to"] != null && m["to"].GetType() != typeof(string))
            {
                throw new Exception("message 'to' attribute must be an address string");
            }

            m["from"] = Symple.buildAddress(this.peer);

            if (m["from"] == m["to"])
            {
                throw new Exception("message sender cannot match the recipient");
            }

            Debug.WriteLine("symple:client: sending" + m);

            string messageJsonString = JsonConvert.SerializeObject(m, Formatting.None);

            this.socket.Send(m);
        }

        public void respond(Dictionary<string, object> m)
        {
            this.send(m, m["from"]);
        }

        public void sendMessage(Dictionary<string, object> m, object to)
        {
            this.send(m, to);
        }

        // sets the client to an error state and disconnects
        public void setError(string error, string message = null)
        {
            Debug.WriteLine("symple:client: fatal error " + error + " " + message);

            this.dispatch("error", error, message);
            if (this.socket != null)
            {
                this.socket.Disconnect();
            }
        }

        // extended dispatch function to handle filtered message response callbacks first, and then standard events
        private void dispatch(string eventLabel, params object[] arguments)
        {
            if (!this.dispatchResponse(eventLabel, arguments))
            {
                base.dispatch(eventLabel, arguments);
            }
        }

        private void sendCommand(Dictionary<string, object> c, object to, Action<object> fn, bool once)
        {
            //c = new SympleCommand(c, to);
            c = new SympleCommand(c); // NOTE: removed "to" since I don't know what to do with it
            this.send(c);

            if (fn != null)
            {
                Dictionary<string, object> filters = new Dictionary<string, object>();
                filters["id"] = c["id"];

                Action<object> after = (res) =>
                {
                    Dictionary<string, object> resObj = (Dictionary<string, object>)res;
                    int status = (int)resObj["status"];

                    if (once || (
                    // 202 (Accepted) and 406 (Not acceptable) responses codes
                    // signal that the command has not yet completed.
                    status != 202 && status != 406)) 
                    {
                        this.clear("command", fn);
                    }
                };

                this.onResponse("command", filters, fn, after);
            }
        }

        private void onResponse(string eventLabel, Dictionary<string, object> filters, Action<object> fn, Action<object> after) {
            if (!this.listeners.ContainsKey(eventLabel))
            {
                this.listeners[eventLabel] = new List<object>();
            }

            if (fn != null)
            {
                Dictionary<string, object> listener = new Dictionary<string, object>();
                listener["fn"] = fn;
                listener["after"] = after;
                listener["filters"] = filters;

                this.listeners[eventLabel].Add(listener);
            }
        }

        // dispatch function for handling filtered message response callbacks
        private bool dispatchResponse(string eventLabel, params object[] arguments)
        {
            var data = arguments;

            if (this.listeners.ContainsKey(eventLabel))
            {
                List<object> listenersForEvent = listeners[eventLabel];
                foreach (object listenerForEvent in listenersForEvent)
                {
                    if (listenerForEvent.GetType() == typeof(Dictionary<string, object>))
                    {
                        Dictionary<string, object> listenerObj = (Dictionary<string, object>)listenerForEvent;
                        if (listenerObj["filters"] != null)
                        {
                            {
                                Dictionary<string, object> filtersObj = (Dictionary<string, object>)listenerObj["filters"];
                                Dictionary<string, object> dataObj = (Dictionary<string, object>)data[0];
                                if (Symple.match(filtersObj, dataObj))
                                {
                                    Action<object> fn = (Action<object>)listenerObj["fn"];
                                    fn(data);
                                    if (listenerObj["after"] != null)
                                    {
                                        Action<object> after = (Action<object>)listenerObj["after"];
                                        after(data);
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
