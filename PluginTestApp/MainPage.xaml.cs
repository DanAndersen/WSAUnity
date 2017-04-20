using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.WebRtc;
using Windows.UI.Core;

using WSAUnity;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PluginTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Plugin p;

        SymplePlayer player = null;
        SympleClient client = null;
        JObject remotePeer;
        bool initialized = false;

        public MainPage()
        {
            this.InitializeComponent();

            p = new Plugin();

            //Messenger.AddListener<string>(SympleLog.LogTrace, OnLog);
            Messenger.AddListener<string>(SympleLog.LogDebug, OnLog);
            Messenger.AddListener<string>(SympleLog.LogInfo, OnLog);
            Messenger.AddListener<string>(SympleLog.LogError, OnLog);
        }

        private void OnLog(string msg)
        {
            Debug.WriteLine(msg);

            // http://stackoverflow.com/questions/19341591/the-application-called-an-interface-that-was-marshalled-for-a-different-thread
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                // Your UI update code goes here!
                textBox.Text += msg + "\n";
            }
            );
            
        }


        private void startPlaybackAndRecording()
        {
            Messenger.Broadcast(SympleLog.LogDebug, "startPlaybackAndRecording");
            JObject playParams = new JObject();   // empty params

            player.play(playParams);

            var engine = (SymplePlayerEngineWebRTC)player.engine;
            engine.sendLocalSDP = (desc) =>
            {
                Messenger.Broadcast(SympleLog.LogDebug, "send offer");
                
                JObject sessionDesc = new JObject();
                sessionDesc["sdp"] = desc.Sdp;
                if (desc.Type == Org.WebRtc.RTCSdpType.Answer)
                {
                    sessionDesc["type"] = "answer";
                } else if (desc.Type == Org.WebRtc.RTCSdpType.Offer)
                {
                    sessionDesc["type"] = "offer";
                } else if (desc.Type == Org.WebRtc.RTCSdpType.Pranswer)
                {
                    sessionDesc["type"] = "pranswer";
                }
                

                JObject parameters = new JObject();
                parameters["to"] = remotePeer;
                parameters["type"] = "message";
                parameters["offer"] = sessionDesc;

                client.send(parameters);
            };
            engine.sendLocalCandidate = (cand) =>
            {
                JObject candidateInit = new JObject();
                candidateInit["candidate"] = cand.Candidate;
                candidateInit["sdpMid"] = cand.SdpMid;
                candidateInit["sdpMLineIndex"] = cand.SdpMLineIndex;

                JObject parameters = new JObject();
                parameters["to"] = remotePeer;
                parameters["type"] = "message";
                parameters["candidate"] = candidateInit;
                
                client.send(parameters);
            };
        }



        private void button_Click(object sender, RoutedEventArgs e)
        {
            JObject CLIENT_OPTIONS = new JObject();
            CLIENT_OPTIONS["secure"] = true;
            CLIENT_OPTIONS["url"] = "https://andersed-talos.ddns.net:443";
            CLIENT_OPTIONS["peer"] = new JObject();
            CLIENT_OPTIONS["peer"]["user"] = "demo";
            CLIENT_OPTIONS["peer"]["name"] = "Demo User";
            CLIENT_OPTIONS["peer"]["group"] = "public";

            SymplePlayerOptions playerOptions = new SymplePlayerOptions();
            playerOptions.engine = "WebRTC";
            playerOptions.initiator = true;

            // WebRTC config
            // This is where you would add TURN servers for use in production
            RTCConfiguration WEBRTC_CONFIG = new RTCConfiguration { IceServers = new List<RTCIceServer> {
                new RTCIceServer { Url = "stun:stun.l.google.com:19302", Username = string.Empty, Credential = string.Empty }
            } };
            
            playerOptions.rtcConfig = WEBRTC_CONFIG;
            //playerOptions.iceMediaConstraints = asdf; // TODO: not using iceMediaConstraints in latest code?
            playerOptions.onStateChange = (player, state, message) =>
            {
                player.displayStatus(state);
            };

            Messenger.Broadcast(SympleLog.LogInfo, "creating player");
            player = new SymplePlayer(playerOptions);

            Messenger.Broadcast(SympleLog.LogInfo, "creating client");
            client = new SympleClient(CLIENT_OPTIONS);

            client.on("announce", (peer) => {
                Messenger.Broadcast(SympleLog.LogInfo, "Authentication success: " + peer);
            });

            client.on("addPeer", (peerObj) =>
            {
                JObject peer = (JObject)peerObj;

                Messenger.Broadcast(SympleLog.LogInfo, "adding peer: " + peer);

                if ((string)peer["user"] == "videorecorder" && !initialized)
                {
                    initialized = true;
                    remotePeer = peer;
                    startPlaybackAndRecording();
                }
            });

            client.on("removePeer", (peer) =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Removing peer: " + peer);
            });

            client.on("message", (mObj) =>
            {
                Messenger.Broadcast(SympleLog.LogTrace, "mObj.GetType().ToString(): " + mObj.GetType().ToString());

                JObject m = (JObject) ((Object[])mObj)[0];

                Messenger.Broadcast(SympleLog.LogTrace, "recv message: " + m);
                Messenger.Broadcast(SympleLog.LogTrace, "remotePeer: " + remotePeer);

                var mFrom = m["from"];

                JToken mFromId = null;

                if (mFrom.Type == JTokenType.Object)
                {
                    mFromId = mFrom["id"];
                }

                if (remotePeer != null && !remotePeer["id"].Equals(mFromId))
                {
                    Messenger.Broadcast(SympleLog.LogInfo, "Dropping message from unknown peer: " + m);
                    return;
                }
                if (m["offer"] != null)
                {
                    Messenger.Broadcast(SympleLog.LogInfo, "Unexpected offer for one-way streaming");
                } else if (m["answer"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                    string answerJsonString = JsonConvert.SerializeObject(m["answer"], Formatting.None);

                    JObject answerParams = (JObject)m["answer"];

                    Messenger.Broadcast(SympleLog.LogTrace, "Receive answer: " + answerJsonString);
                    engine.recvRemoteSDP(answerParams);
                } else if (m["candidate"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                    JObject candidateParams = (JObject) m["candidate"];

                    engine.recvRemoteCandidate(candidateParams);
                }
            });

            client.on("disconnect", (peer) =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Disconnected from server");
            });

            client.on("error", (error) =>
            {
                Messenger.Broadcast(SympleLog.LogError, "Connection error: " + error);
            });

            client.connect();






            string status = p.GetStatus();

            textBox.Text = status;
        }
    }
}
