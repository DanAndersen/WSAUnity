using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

#if NETFX_CORE
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.Devices;
using Org.WebRtc;
#endif

namespace WSAUnity
{
    public class Plugin
    {
        SymplePlayer player = null;
        SympleClient client = null;
#if NETFX_CORE
        JObject remotePeer;
#endif
        bool initialized = false;
        
        public void initAndStartWebRTC()
        {
#if NETFX_CORE

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
            RTCConfiguration WEBRTC_CONFIG = new RTCConfiguration
            {
                IceServers = new List<RTCIceServer> {
                new RTCIceServer { Url = "stun:stun.l.google.com:19302", Username = string.Empty, Credential = string.Empty }
            }
            };

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

                JObject m = (JObject)((Object[])mObj)[0];

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
                }
                else if (m["answer"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                    string answerJsonString = JsonConvert.SerializeObject(m["answer"], Formatting.None);

                    JObject answerParams = (JObject)m["answer"];

                    Messenger.Broadcast(SympleLog.LogTrace, "Receive answer: " + answerJsonString);
                    engine.recvRemoteSDP(answerParams);
                }
                else if (m["candidate"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                    JObject candidateParams = (JObject)m["candidate"];

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
#else
            Messenger.Broadcast(SympleLog.LogInfo, "not actually connecting via webrtc because NETFX_CORE not defined (probably this is in the unity editor)");
#endif
        }


#if NETFX_CORE
        public async void basicTestVideo()
        {
            Messenger.Broadcast(SympleLog.LogDebug, "basicTestVideo()");

            WebRTC.Initialize(null);    // needed before calling any webrtc functions http://stackoverflow.com/questions/43331677/webrtc-for-uwp-new-rtcpeerconnection-doesnt-complete-execution

            Messenger.Broadcast(SympleLog.LogDebug, "creating media");
            Media media = Media.CreateMedia();
            media.OnMediaDevicesChanged += (MediaDeviceType mediaType) =>
            {
                Messenger.Broadcast(SympleLog.LogDebug, "OnMediaDevicesChanged(), mediaType = " + mediaType);
            };
            Messenger.Broadcast(SympleLog.LogDebug, "created media");

            var videoCaptureDevices = media.GetVideoCaptureDevices();
            Messenger.Broadcast(SympleLog.LogDebug, "num videoCaptureDevices: " + videoCaptureDevices.Count);

            var videoDevice = videoCaptureDevices[0];

            Messenger.Broadcast(SympleLog.LogDebug, "getting videoCaptureCapabilities");
            var videoCaptureCapabilities = await videoDevice.GetVideoCaptureCapabilities();
            Messenger.Broadcast(SympleLog.LogDebug, "got videoCaptureCapabilities");

            var chosenCapability = videoCaptureCapabilities[0];
            Messenger.Broadcast(SympleLog.LogDebug, "chosenCapability:");
            Messenger.Broadcast(SympleLog.LogDebug, "\tWidth: " + (int)chosenCapability.Width);
            Messenger.Broadcast(SympleLog.LogDebug, "\tHeight: " + (int)chosenCapability.Height);
            Messenger.Broadcast(SympleLog.LogDebug, "\tFrameRate: " + (int)chosenCapability.FrameRate);
            WebRTC.SetPreferredVideoCaptureFormat((int)chosenCapability.Width, (int)chosenCapability.Height, (int)chosenCapability.FrameRate);

            Messenger.Broadcast(SympleLog.LogDebug, "getting usermedia");
            MediaStream localStream = await media.GetUserMedia(new RTCMediaStreamConstraints { videoEnabled = true, audioEnabled = true });
            Messenger.Broadcast(SympleLog.LogDebug, "got usermedia");

            var videoTracks = localStream.GetVideoTracks();
            Messenger.Broadcast(SympleLog.LogDebug, "num videoTracks: " + videoTracks.Count);

            var source = media.CreateMediaSource(videoTracks[0], Symple.LocalMediaStreamId);
            Messenger.Broadcast(SympleLog.LogDebug, "created mediasource");

            Messenger.Broadcast(SympleLog.MediaSource, source);
        }
#endif



        private void startPlaybackAndRecording()
        {
#if NETFX_CORE
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
                }
                else if (desc.Type == Org.WebRtc.RTCSdpType.Offer)
                {
                    sessionDesc["type"] = "offer";
                }
                else if (desc.Type == Org.WebRtc.RTCSdpType.Pranswer)
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
#else
            Messenger.Broadcast(SympleLog.LogInfo, "not actually doing startPlaybackAndRecording because NETFX_CORE not defined (probably this is in the unity editor)");
#endif
        }
    }
}
