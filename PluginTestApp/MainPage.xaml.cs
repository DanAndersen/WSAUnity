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

        SymplePlayer player;
        SympleClient client;
        SymplePeer remotePeer;
        bool initialized = false;

        public MainPage()
        {
            this.InitializeComponent();

            p = new Plugin();
        }


        private void startPlaybackAndRecording()
        {
            player.play();
            player.engine.sendLocalSDP = (desc) =>
            {
                Debug.WriteLine("send offer: " + JSON.stringify(desc));

                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["to"] = remotePeer;
                parameters["type"] = "message";
                parameters["offer"] = desc;

                client.send(parameters);
            };
            player.engine.sendLocalCandidate = (cand) =>
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["to"] = remotePeer;
                parameters["type"] = "message";
                parameters["candidate"] = cand;
                
                client.send(parameters);
            };
        }



        private void button_Click(object sender, RoutedEventArgs e)
        {
            SympleClientOptions CLIENT_OPTIONS = new SympleClientOptions();
            CLIENT_OPTIONS.secure = true;
            CLIENT_OPTIONS.url = "https://andersed-talos.ddns.net:443";
            CLIENT_OPTIONS.peer = new SymplePeer(){ user = "demo", name = "Demo User", group = "public"};


            SymplePlayerOptions playerOptions = new SymplePlayerOptions();
            playerOptions.engine = "WebRTC";
            playerOptions.initiator = true;
            playerOptions.rtcConfig = WEBRTC_CONFIG;
            playerOptions.iceMediaConstraints = asdf;
            playerOptions.onStateChange = (player, state, message) =>
            {
                player.displayStatus(state);
            };


            player = new SymplePlayer(playerOptions);

            client = new SympleClient(CLIENT_OPTIONS);

            client.on("announce", (peer) => {
                Debug.WriteLine("Authentication success: " + peer);
            });

            client.on("addPeer", (peerObj) =>
            {
                Dictionary<string, object> peer = (Dictionary<string, object>)peerObj;

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
                Dictionary<string, object> m = (Dictionary<string, object>)mObj;

                Debug.WriteLine("recv message: " + m);

                Dictionary<string, object> from = (Dictionary<string, object>)m["from"];

                if (remotePeer != null && remotePeer.id != from["id"])
                {
                    Debug.WriteLine("Dropping message from unknown peer: " + m);
                    return;
                }
                if (m["offer"] != null)
                {
                    Debug.WriteLine("Unexpected offer for one-way streaming");
                } else if (m["answer"] != null)
                {
                    string answerJsonString = JsonConvert.SerializeObject(m["answer"], Formatting.None);

                    Debug.WriteLine("Receive answer: " + answerJsonString);
                    player.engine.recvRemoteSDP(m["answer"]);
                } else if (m["candidate"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;
                    engine.recvRemoteCandidate(m["candidate"]);
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
