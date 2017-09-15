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
    public class StarWebrtcContext
    {
        public enum StarUserType
        {
            TRAINEE,
            MENTOR
        }

        public StarUserType UserType { get; private set; }

        /// <summary>
        /// The WebRTC signalling server used by all peers to coordinate connections with each other.
        /// </summary>
        public string SignallingServerUrl { get; set; } = "https://purduestarproj-webrtc-signal.herokuapp.com";

        /// <summary>
        /// TRAINEE only: The peer username of the remote mentor user that this context will wait for.
        /// Once another peer with this name connects, the TRAINEE context will automatically start sending video to that peer.
        /// </summary>
        public string ExpectedRemoteReceiverUsername { get; set; } = "star-mentor";

        /// <summary>
        /// The username that this peer is known by in the WebRTC signalling server.
        /// </summary>
        public string LocalPeerUsername { get; private set; }

        /// <summary>
        /// The full name (purely cosmetic) that this peer is known by in the WebRTC signalling server.
        /// </summary>
        public string LocalPeerNameLabel { get; set; }

        public string LocalPeerGroup { get; set; } = "public";
        
        public static StarWebrtcContext CreateTraineeContext()
        {
            StarWebrtcContext ctx = new StarWebrtcContext();
            ctx.UserType = StarUserType.TRAINEE;
            ctx.LocalPeerUsername = "star-trainee";
            ctx.LocalPeerNameLabel = "STAR Trainee";
            ctx.ExpectedRemoteReceiverUsername = "star-mentor";

            return ctx;
        }

        public static StarWebrtcContext CreateMentorContext(string overrideLocalPeerUsername = null)
        {
            StarWebrtcContext ctx = new StarWebrtcContext();
            ctx.UserType = StarUserType.MENTOR;
            ctx.LocalPeerUsername = "star-mentor";
            ctx.LocalPeerNameLabel = "STAR Mentor";

            return ctx;
        }


        private StarWebrtcContext()
        {

        }



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
            CLIENT_OPTIONS["url"] = this.SignallingServerUrl;
            CLIENT_OPTIONS["peer"] = new JObject();
            CLIENT_OPTIONS["peer"]["user"] = this.LocalPeerUsername;
            CLIENT_OPTIONS["peer"]["name"] = this.LocalPeerNameLabel;
            CLIENT_OPTIONS["peer"]["group"] = this.LocalPeerGroup;

            SymplePlayerOptions playerOptions = new SymplePlayerOptions();
            playerOptions.engine = "WebRTC";

            switch (UserType)
            {
                case StarUserType.TRAINEE:
                    playerOptions.initiator = true;
                    break;
                case StarUserType.MENTOR:
                    playerOptions.initiator = false;
                    break;
                default:
                    break;
            }
            

            // WebRTC config
            // This is where you would add TURN servers for use in production
            RTCConfiguration WEBRTC_CONFIG = new RTCConfiguration
            {
                IceServers = new List<RTCIceServer> {
                    new RTCIceServer { Url = "stun:stun.l.google.com:19302", Username = string.Empty, Credential = string.Empty },
                    new RTCIceServer { Url = "stun:stun1.l.google.com:19302", Username = string.Empty, Credential = string.Empty },
                    new RTCIceServer { Url = "stun:stun2.l.google.com:19302", Username = string.Empty, Credential = string.Empty },
                    new RTCIceServer { Url = "stun:stun3.l.google.com:19302", Username = string.Empty, Credential = string.Empty },
                    new RTCIceServer { Url = "stun:stun4.l.google.com:19302", Username = string.Empty, Credential = string.Empty },
                    new RTCIceServer { Url = "turn:numb.viagenie.ca", Username = "purduestarproj@gmail.com", Credential = "0O@S&YfP$@56" }
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

                if (this.UserType == StarUserType.TRAINEE)
                {
                    // the TRAINEE user waits for a peer with a specific username, then once it's connected it automatically starts sending video

                    if ((string)peer["user"] == this.ExpectedRemoteReceiverUsername && !initialized)
                    {
                        initialized = true;
                        remotePeer = peer;
                        startPlaybackAndRecording();
                    }
                }
                
            });

            client.on("presence", (presence) =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Recv presence: " + presence);
            });

            client.on("removePeer", (peerObj) =>
            {
                JObject peer = (JObject)peerObj;

                Messenger.Broadcast(SympleLog.LogInfo, "Removing peer: " + peer);

                if (remotePeer != null && remotePeer["id"].Equals(peer["id"]))
                {
                    initialized = false;
                    remotePeer = null;
                    player.engine.destroy();
                    player.engine = null;
                }
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
                    switch (UserType)
                    {
                        case StarUserType.TRAINEE:
                            Messenger.Broadcast(SympleLog.LogInfo, "Unexpected offer for one-way streaming");
                            break;
                        case StarUserType.MENTOR:

                            Messenger.Broadcast(SympleLog.LogInfo, "Receive offer: " + m["offer"]);

                            remotePeer = (JObject)m["from"];

                            JObject playParams = new JObject();   // empty params
                            player.play(playParams);

                            var engine = (SymplePlayerEngineWebRTC)player.engine;

                            engine.recvRemoteSDP((JObject)m["offer"]);

                            engine.sendLocalSDP = (desc) =>
                            {
                                Messenger.Broadcast(SympleLog.LogInfo, "Send answer: " + desc);

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
                                parameters["answer"] = sessionDesc;

                                client.send(parameters);
                            };

                            engine.sendLocalCandidate = (cand) =>
                            {
                                JObject candidateObj = new JObject();
                                candidateObj["candidate"] = cand.Candidate;
                                candidateObj["sdpMid"] = cand.SdpMid;
                                candidateObj["sdpMLineIndex"] = cand.SdpMLineIndex;

                                JObject parameters = new JObject();
                                parameters["to"] = remotePeer;
                                parameters["type"] = "message";
                                parameters["candidate"] = candidateObj;

                                client.send(parameters);
                            };

                            break;
                        default:
                            break;
                    }
                    
                }
                else if (m["answer"] != null)
                {
                    switch (UserType)
                    {
                        case StarUserType.TRAINEE:

                            SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                            string answerJsonString = JsonConvert.SerializeObject(m["answer"], Formatting.None);

                            JObject answerParams = (JObject)m["answer"];

                            Messenger.Broadcast(SympleLog.LogTrace, "Receive answer: " + answerJsonString);
                            engine.recvRemoteSDP(answerParams);

                            break;
                        case StarUserType.MENTOR:

                            Messenger.Broadcast(SympleLog.LogInfo, "Unexpected answer for one-way streaming");

                            break;
                        default:
                            break;
                    }                    
                }
                else if (m["candidate"] != null)
                {
                    SymplePlayerEngineWebRTC engine = (SymplePlayerEngineWebRTC)player.engine;

                    JObject candidateParams = (JObject)m["candidate"];

                    Messenger.Broadcast(SympleLog.LogInfo, "Using Candidate: " + candidateParams);
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

        private bool webrtcInitialized = false;


#if NETFX_CORE
        private Media _media;
        private MediaVideoTrack _selectedVideoTrack;
        private MediaStream _localStream;
        public async void testStartVideoLocal()
        {
            Messenger.Broadcast(SympleLog.LogDebug, "basicTestVideo()");
            
            if (!webrtcInitialized)
            {
                WebRTC.Initialize(null);    // needed before calling any webrtc functions http://stackoverflow.com/questions/43331677/webrtc-for-uwp-new-rtcpeerconnection-doesnt-complete-execution
                webrtcInitialized = true;
            }
            
            Messenger.Broadcast(SympleLog.LogDebug, "creating media");

            if (_media == null)
            {
                _media = Media.CreateMedia();
                _media.OnMediaDevicesChanged += (MediaDeviceType mediaType) =>
                {
                    Messenger.Broadcast(SympleLog.LogDebug, "OnMediaDevicesChanged(), mediaType = " + mediaType);
                };
            }
            
            Messenger.Broadcast(SympleLog.LogDebug, "created media");

            var videoCaptureDevices = _media.GetVideoCaptureDevices();
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
            if (_localStream == null)
            {
                _localStream = await _media.GetUserMedia(new RTCMediaStreamConstraints { videoEnabled = true, audioEnabled = true });
            }
            
            Messenger.Broadcast(SympleLog.LogDebug, "got usermedia");

            Messenger.Broadcast(SympleLog.LogDebug, "localStream id: " + _localStream.Id);
            Messenger.Broadcast(SympleLog.LogDebug, "localStream Active?: " + _localStream.Active);

            var videoTracks = _localStream.GetVideoTracks();
            Messenger.Broadcast(SympleLog.LogDebug, "num videoTracks: " + videoTracks.Count);

            _selectedVideoTrack = videoTracks[0];

            Messenger.Broadcast(SympleLog.LogDebug, "selected video track id: " + _selectedVideoTrack.Id);
            Messenger.Broadcast(SympleLog.LogDebug, "selected video track suspended?: " + _selectedVideoTrack.Suspended);
            Messenger.Broadcast(SympleLog.LogDebug, "selected video track enabled?: " + _selectedVideoTrack.Enabled);

            var source = _media.CreateMediaSource(_selectedVideoTrack, Symple.LocalMediaStreamId);

            Messenger.Broadcast(SympleLog.LogDebug, "created mediasource");

            Messenger.Broadcast(SympleLog.CreatedMediaSource, source);
        }

        public async void testShutdownVideoLocal()
        {
            Messenger.Broadcast(SympleLog.DestroyedMediaSource);
        }
#endif

    }
}
