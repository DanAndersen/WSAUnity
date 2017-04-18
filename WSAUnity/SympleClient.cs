﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SympleClient : SympleDispatcher
    {
        private Socket socket;
        SympleRoster roster;

        JObject options;

        IO.Options ioOptions;
        JObject peer;

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


            Debug.WriteLine("done initing SympleClient, values: ");
            Debug.WriteLine("this.peer: " + this.peer.ToString());
        }

        public void connect()
        {
            Debug.WriteLine("symple:client: connecting");

            if (this.socket != null)
            {
                throw new Exception("the client socket is not null");
            }

            this.socket = IO.Socket(this.options["url"].ToString(), this.ioOptions);
            this.socket.On(Socket.EVENT_CONNECT, () => {
                Debug.WriteLine("ssymple:client: connected");

                JObject announceData = new JObject();
                announceData["user"] = this.peer["user"] ?? "";
                announceData["name"] = this.peer["name"] ?? "";
                announceData["type"] = this.peer["type"] ?? "";
                announceData["token"] = options["token"] ?? "";

                string announceDataJsonString = JsonConvert.SerializeObject(announceData, Formatting.None);
                Debug.WriteLine("announceDataJsonString: " + announceDataJsonString);

                this.socket.Emit("announce", (resObj) => {
                    Debug.WriteLine("symple:client: announced " + resObj);

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
                        Debug.WriteLine("symple:client receive " + msg);

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

                                if ((bool)m["probe"])
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
                            Debug.WriteLine("symple:client: invalid sender address: " + m);
                            return;
                        }

                        // replace the from attribute with the full peer object.
                        // this will only work for peer messages, not server messages.
                        var rpeer = this.roster.get((string)m["from"]);
                        if (rpeer != null)
                        {
                            m["from"] = rpeer;
                        } else
                        {
                            Debug.WriteLine("symple:client: got message from unknown peer: " + m);
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

        public void join(JObject room)
        {
            this.socket.Emit("join", room);
        }

        public void leave(JObject room)
        {
            this.socket.Emit("leave", room);
        }

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

        // send a message to the given peer
        // m = JSON object
        // to = either a string or a JSON object to build an address from
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

            Debug.WriteLine("symple:client: sending" + m);

            string messageJsonString = JsonConvert.SerializeObject(m, Formatting.None);

            this.socket.Send(m);
        }

        public void respond(JObject m)
        {
            this.send(m, m["from"]);
        }

        public void sendMessage(JObject m, JToken to)
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
    }
}
