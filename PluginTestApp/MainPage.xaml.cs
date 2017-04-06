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
        string remotePeer;
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
                client.send({ to: remotePeer, type: "message", offer: desc });
            };
            player.engine.sendLocalCandidate = (cand) =>
            {
                client.send({ to: remotePeer, type: "message", candidate: cand });
            };
        }



        private void button_Click(object sender, RoutedEventArgs e)
        {
            SympleClientOptions CLIENT_OPTIONS = new SympleClientOptions();
            CLIENT_OPTIONS.secure = true;
            CLIENT_OPTIONS.url = "https://andersed-talos.ddns.net:443";
            CLIENT_OPTIONS.peer = new SymplePeer(){ user = "demo", name = "Demo User", group = "public"};


            player = new SymplePlayer();

            client = new SympleClient(CLIENT_OPTIONS);

            client.on("announce", (peer) => {
                Debug.WriteLine("Authentication success: " + peer);
            });

            client.on("addPeer", (peer) =>
            {
                Debug.WriteLine("adding peer: " + peer);

                if (peer.user == "videorecorder" && !initialized)
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

            client.on("message", (m) =>
            {
                Debug.WriteLine("recv message: " + m);

                if (remotePeer != null && remotePeer.id != m.from.id)
                {
                    Debug.WriteLine("Dropping message from unknown peer: " + m);
                    return;
                }
                if (m.offer)
                {
                    Debug.WriteLine("Unexpected offer for one-way streaming");
                } else if (m.answer)
                {
                    Debug.WriteLine("Receive answer: " + JSON.stringify(m.answer));
                    player.engine.recvRemoteSDP(m.answer);
                } else if (m.candidate)
                {
                    player.engine.recvRemoteCandidate(m.candidate);
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
