using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
#if NETFX_CORE
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

#if NETFX_CORE
using Quobject.SocketIoClientDotNet.Client;
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SympleClient : SympleDispatcher
    {
#if NETFX_CORE
        private Socket socket;
        IO.Options ioOptions;
        JObject options;
        JObject peer;
#endif
        SympleRoster roster;





#if NETFX_CORE
        public SympleClient(JObject options) : base()
        {

            this.options = options;
            options["url"] = options["url"] ?? "http://localhost:4000";
            options["secure"] = (options["url"] != null && (options["url"].ToString().IndexOf("https") == 0 || options["url"].ToString().IndexOf("wss") == 0));
            options["token"] = null;

            this.peer = options["peer"] as JObject ?? new JObject();
            this.peer["rooms"] = options["peer"]["rooms"] ?? new JArray();
            this.roster = new SympleRoster(this);
            this.socket = null;

            Uri socketUri = new Uri(options["url"].ToString());


            ioOptions = new IO.Options();
            ioOptions.Secure = (bool) options["secure"];
            ioOptions.Port = socketUri.Port;
            ioOptions.Hostname = socketUri.Host;
            ioOptions.IgnoreServerCertificateValidation = true;


            Messenger.Broadcast(SympleLog.LogTrace, "done initing SympleClient, values: ");
            Messenger.Broadcast(SympleLog.LogTrace, "this.peer: " + this.peer.ToString());

        }
#endif

        public void connect()
        {
#if NETFX_CORE
            Messenger.Broadcast(SympleLog.LogInfo, "symple:client: connecting");

            if (this.socket != null)
            {
                string err = "the client socket is not null";
                Messenger.Broadcast(SympleLog.LogError, err);
                throw new Exception(err);
            }

            this.socket = IO.Socket(this.options["url"].ToString(), this.ioOptions);
            this.socket.On(Socket.EVENT_CONNECT, () => {
                Messenger.Broadcast(SympleLog.LogInfo, "symple:client: connected");
                Messenger.Broadcast(SympleLog.Connected);

                JObject announceData = new JObject();
                announceData["user"] = this.peer["user"] ?? "";
                announceData["name"] = this.peer["name"] ?? "";
                announceData["type"] = this.peer["type"] ?? "";
                announceData["token"] = options["token"] ?? "";

                string announceDataJsonString = JsonConvert.SerializeObject(announceData, Formatting.None);
                Messenger.Broadcast(SympleLog.LogTrace, "announceDataJsonString: " + announceDataJsonString);

                this.socket.Emit("announce", (resObj) => {
                    Messenger.Broadcast(SympleLog.LogDebug, "symple:client: announced " + resObj);
                    Messenger.Broadcast(SympleLog.Announced);

                    JObject res = (JObject) resObj;

                    if ((int)res["status"] != 200)
                    {
                        this.setError("auth", resObj.ToString());
                        return;
                    }

                    JObject resData = (JObject) res["data"];

                    this.peer = Symple.extend(this.peer, resData);
                    this.roster.add(resData);

                    JObject sendPresenceParams = new JObject();
                    sendPresenceParams["probe"] = true;

                    this.sendPresence(sendPresenceParams);
                    this.dispatch("announce", res);
                    this.socket.On(Socket.EVENT_MESSAGE, (msg) =>
                    {
                        Messenger.Broadcast(SympleLog.LogTrace, "symple:client receive " + msg);

                        JObject m = (JObject)msg;

                        string mType = (string)m["type"];

                        switch (mType)
                        {
                            case "message":
                                m = new SympleMessage(m);
                                break;
                            case "command":
                                m = new SympleCommand(m);
                                break;
                            case "event":
                                m = new SympleEvent(m);
                                break;
                            case "presence":
                                m = new SymplePresence(m);
                                if ((bool)m["data"]["online"])
                                {
                                    this.roster.update((JObject)m["data"]);
                                } else
                                {
                                    this.roster.remove((string)m["data"]["id"]);
                                }

                                if (m["probe"] != null && (bool)m["probe"] == true)
                                {
                                    JObject presenceTo = new JObject();
                                    presenceTo["to"] = Symple.parseAddress(m["from"].ToString())["id"];

                                    this.sendPresence(new SymplePresence(presenceTo));
                                }
                                break;
                            default:
                                var o = m;
                                o["type"] = o["type"] ?? "message";
                                break;
                        }

                        if (m["from"].Type != JTokenType.String)
                        {
                            Messenger.Broadcast(SympleLog.LogError, "symple:client: invalid sender address: " + m);
                            return;
                        }

                        // replace the from attribute with the full peer object.
                        // this will only work for peer messages, not server messages.

                        string mFrom = (string)m["from"];
                        Messenger.Broadcast(SympleLog.LogTrace, "looking up rpeer in roster, mFrom = " + mFrom + "...");
                        
                        var rpeer = this.roster.get(mFrom);
                        if (rpeer != null)
                        {
                            Messenger.Broadcast(SympleLog.LogTrace, "found rpeer: " + rpeer);
                            m["from"] = rpeer;
                        } else
                        {
                            Messenger.Broadcast(SympleLog.LogInfo, "symple:client: got message from unknown peer: " + m);
                        }

                        // Dispatch to the application
                        this.dispatch((string)m["type"], m);
                    });


                }, announceData);
            });

            this.socket.On(Socket.EVENT_ERROR, () =>
            {
                // this is triggered when any transport fails, so not necessarily fatal
                this.dispatch("connect");
            });

            this.socket.On("connecting", () =>
            {
                Messenger.Broadcast(SympleLog.LogDebug, "symple:client: connecting");
                this.dispatch("connecting");
            });

            this.socket.On(Socket.EVENT_RECONNECTING, () =>
            {
                Messenger.Broadcast(SympleLog.LogDebug, "symple:client: connecting");
                Messenger.Broadcast(SympleLog.Reconnecting);
                this.dispatch("reconnecting");
            });

            this.socket.On("connect_failed", () =>
            {
                // called when all transports fail
                Messenger.Broadcast(SympleLog.LogError, "symple:client: connect failed");
                Messenger.Broadcast(SympleLog.ConnectFailed);
                this.dispatch("connect_failed");
                this.setError("connect");
            });

            this.socket.On(Socket.EVENT_DISCONNECT, () =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "symple:client: disconnect");
                Messenger.Broadcast(SympleLog.Disconnect);
                this.peer["online"] = false;
                this.dispatch("disconnect");
            });
#endif
        }

        // disconnect from the server
        public void disconnect()
        {
#if NETFX_CORE
            if (this.socket != null)
            {
                this.socket.Disconnect();
            }
#endif
        }

        // return the online status
        public bool online()
        {
#if NETFX_CORE
            return (bool) this.peer["online"];
#else
            return false;
#endif
        }

#if NETFX_CORE
        public void join(JObject room)
        {
            this.socket.Emit("join", room);
        }
#endif

#if NETFX_CORE
        public void leave(JObject room)
        {
            this.socket.Emit("leave", room);
        }
#endif

#if NETFX_CORE
        public void sendPresence(JObject p)
        {
            p = p ?? new JObject();
            if (p["data"] != null)
            {
                JObject pDataObj = (JObject)p["data"];
                p["data"] = Symple.merge(this.peer, pDataObj);
            } else
            {
                p["data"] = this.peer;
            }
            this.send(new SymplePresence(p));
        }
#endif

        // send a message to the given peer
        // m = JSON object
        // to = either a string or a JSON object to build an address from
#if NETFX_CORE
        public void send(JObject m, JToken to = null)
        {

            if (!this.online())
            {
                throw new Exception("cannot send messages while offline"); // TODO: add to pending queue?
            }
            
            if (m.Type != JTokenType.Object)
            {
                throw new Exception("message must be an object");
            }

            if (m["type"].Type != JTokenType.String)
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

            if (m["to"] != null && m["to"].Type == JTokenType.Object)
            {
                JObject mToObj = (JObject)m["to"];
                m["to"] = Symple.buildAddress(mToObj);
            }

            if (m["to"] != null && m["to"].Type != JTokenType.String)
            {
                throw new Exception("message 'to' attribute must be an address string");
            }

            m["from"] = Symple.buildAddress(this.peer);

            if (m["from"] == m["to"])
            {
                throw new Exception("message sender cannot match the recipient");
            }

            Messenger.Broadcast(SympleLog.LogTrace, "symple:client: sending" + m);
            
            this.socket.Send(m);
        }
#endif

#if NETFX_CORE
        public void respond(JObject m)
        {
            this.send(m, m["from"]);
        }
#endif

#if NETFX_CORE
        public void sendMessage(JObject m, JToken to)
        {
            this.send(m, to);
        }
#endif

        // sets the client to an error state and disconnects
        public void setError(string error, string message = null)
        {
#if NETFX_CORE
            Messenger.Broadcast(SympleLog.LogError, "symple:client: fatal error " + error + " " + message);

            this.dispatch("error", error, message);
            if (this.socket != null)
            {
                this.socket.Disconnect();
            }
#endif
        }

#if NETFX_CORE
        // extended dispatch function to handle filtered message response callbacks first, and then standard events
        private void dispatch(string eventLabel, params object[] arguments)
        {
            if (!this.dispatchResponse(eventLabel, arguments))
            {
                base.dispatch(eventLabel, arguments);
            }
        }
#endif

#if NETFX_CORE
        private void sendCommand(JObject c, object to, Action<object> fn, bool once)
        {
            //c = new SympleCommand(c, to);
            c = new SympleCommand(c); // NOTE: removed "to" since I don't know what to do with it
            this.send(c);

            if (fn != null)
            {
                JObject filters = new JObject();
                filters["id"] = c["id"];

                Action<object> after = (res) =>
                {
                    JObject resObj = (JObject)res;
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
#endif

#if NETFX_CORE
        private void onResponse(string eventLabel, JObject filters, Action<object> fn, Action<object> after) {
            if (!this.listeners.ContainsKey(eventLabel))
            {
                this.listeners[eventLabel] = new List<object>();
            }

            if (fn != null)
            {
                JObjectWithActions listener = new JObjectWithActions();
                listener.actions["fn"] = fn;
                listener.actions["after"] = after;
                listener["filters"] = filters;

                this.listeners[eventLabel].Add(listener);
            }
        }
#endif

#if NETFX_CORE
        // dispatch function for handling filtered message response callbacks
        private bool dispatchResponse(string eventLabel, params object[] arguments)
        {
            var data = arguments;

            if (this.listeners.ContainsKey(eventLabel))
            {
                List<object> listenersForEvent = listeners[eventLabel];
                foreach (object listenerForEvent in listenersForEvent)
                {
                    if (listenerForEvent.GetType() == typeof(JObjectWithActions))
                    {
                        JObjectWithActions listenerObj = (JObjectWithActions)listenerForEvent;
                        if (listenerObj["filters"] != null)
                        {
                            {
                                JObject filtersObj = (JObject)listenerObj["filters"];
                                JObject dataObj = (JObject)data[0];
                                if (Symple.match(filtersObj, dataObj))
                                {
                                    Action<object> fn = listenerObj.actions["fn"];
                                    fn(data);
                                    if (listenerObj["after"] != null)
                                    {
                                        Action<object> after = listenerObj.actions["after"];
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
#endif
    }
}
