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
        }


        private void startPlaybackAndRecording()
        {
            Debug.WriteLine("startPlaybackAndRecording");
            JObject playParams = new JObject();   // empty params

            player.play(playParams);

            var engine = (SymplePlayerEngineWebRTC)player.engine;
            engine.sendLocalSDP = (desc) =>
            {
                Debug.WriteLine("send offer");


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
            Org.WebRtc.RTCConfiguration WEBRTC_CONFIG = new Org.WebRtc.RTCConfiguration();
            WEBRTC_CONFIG.IceServers = new List<Org.WebRtc.RTCIceServer>();

            playerOptions.rtcConfig = WEBRTC_CONFIG;
            //playerOptions.iceMediaConstraints = asdf; // TODO: not using iceMediaConstraints in latest code?
            playerOptions.onStateChange = (player, state, message) =>
            {
                player.displayStatus(state);
            };

#if NETFX_CORE
            Debug.WriteLine("fooasdfasdf");
#else
            Debug.WriteLine("bar");
#endif

            Debug.WriteLine("creating player");
            player = new SymplePlayer(playerOptions);

            client = new SympleClient(CLIENT_OPTIONS);

            client.on("announce", (peer) => {
                Debug.WriteLine("Authentication success: " + peer);
            });

            client.on("addPeer", (peerObj) =>
            {
                JObject peer = (JObject)peerObj;

                Debug.WriteLine("adding peer: " + peer);

                if ((string)peer["user"] == "videorecorder" && !initialized)
                {
                    initialized = true;
                    remotePeer = peer;
                    startPlaybackAndRecording();
                }
            });

            client.on("removePeer", (peer) =>
            {
                Debug.WriteLine("Removing peer: " + peer);
            });

            client.on("message", (mObj) =>
            {
                JObject m = (JObject)mObj;

                Debug.WriteLine("recv message: " + m);

                JObject from = (JObject)m["from"];

                if (remotePeer != null && remotePeer["id"] != from["id"])
                {
                    Debug.WriteLine("Dropping message from unknown peer: " + m);
                    return;
                }
                if (m["offer"] != null)
                {
                    Debug.WriteLine("Unexpected offer for one-way streaming");
                } else if (m["answer"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                    string answerJsonString = JsonConvert.SerializeObject(m["answer"], Formatting.None);

                    JObject answerParams = (JObject)m["answer"];

                    Debug.WriteLine("Receive answer: " + answerJsonString);
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
                Debug.WriteLine("Disconnected from server");
            });

            client.on("error", (error) =>
            {
                Debug.WriteLine("Connection error: " + error);
            });

            client.connect();






            string status = p.GetStatus();

            textBox.Text = status;
        }
    }
}
