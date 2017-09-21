using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


#if NETFX_CORE
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Org.WebRtc;
using Windows.Media.Playback;
using Windows.Media.Core;
#endif

namespace WSAUnity
{
    public class SymplePlayerEngineWebRTC : SymplePlayerEngine
    {
        
        bool initiator;

        const string RemotePeerVideoTrackId = "remote_peer_video_track_id";

#if NETFX_CORE
        private bool webrtcInitialized = false;
        private RTCConfiguration rtcConfig;
        private RTCPeerConnection pc;
        private MediaStream activeStream;
        private Media _media;
        private RTCMediaStreamConstraints userMediaConstraints;
        private MediaStream _localStream;   // hold onto a reference to the local stream even if remote user disconnects
#endif

        public override bool support()
        {
#if NETFX_CORE
            return true;
#else
            return false;
#endif
        }

        public SymplePlayerEngineWebRTC(SymplePlayer player) : base(player)
        {
            Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: init");

#if NETFX_CORE
            if (!webrtcInitialized)
            {
                WebRTC.Initialize(null);    // needed before calling any webrtc functions http://stackoverflow.com/questions/43331677/webrtc-for-uwp-new-rtcpeerconnection-doesnt-complete-execution
                webrtcInitialized = true;
            }
            

            if (player.options.rtcConfig != null)
            {
                this.rtcConfig = player.options.rtcConfig;
            } else
            {
                this.rtcConfig = new RTCConfiguration();
                this.rtcConfig.IceServers.Add(new RTCIceServer() { Url = "stun:stun.l.google.com:19302", Username = string.Empty, Credential = string.Empty });
            }
#endif

            /*
            this.rtcOptions = player.options.rtcOptions || {
                optional: [
                    { DtlsSrtpKeyAgreement: true } // required for FF <=> Chrome interop
                ]
            };
            */

            // Specifies that this client will be the ICE initiator,
            // and will be sending the initial SDP Offer.
            this.initiator = player.options.initiator;
            Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: constructor, set this.initiator to " + this.initiator);

#if NETFX_CORE
            
            // Reference to the active local or remote media stream
            this.activeStream = null;
#endif
        }


#if NETFX_CORE
        private Media GetMedia()
        {
            if (_media == null)
            {
                _media = Media.CreateMedia();
                Media.SetDisplayOrientation(Windows.Graphics.Display.DisplayOrientations.Landscape);
            }
            return _media;
        }
#endif

        public override void setup()
        {
            Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: setup");

#if NETFX_CORE
            Messenger.Broadcast(SympleLog.LogTrace, "before _createPeerConnection");
            this._createPeerConnection();
            Messenger.Broadcast(SympleLog.LogTrace, "after _createPeerConnection");
#endif
        }

        public override void destroy()
        {
            Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: destroy");

#if NETFX_CORE
            this.sendLocalSDP = null;
            this.sendLocalCandidate = null;

            /*
            if (this.pc != null && this.activeStream != null)
            {
                this.pc.RemoveStream(this.activeStream);
            }
            */

            Messenger.Broadcast(SympleLog.DestroyedMediaSource);

            this.activeStream = null; // TODO: needs explicit close?
#endif

            /*
            if (this.video != null)
            {
                this.video.src = "";
                this.video = null;
                // anything else needed for video cleanup?
            }
            */

#if NETFX_CORE
            if (this.pc != null)
            {
                this.pc.Close();
                this.pc = null;
                // anything else needed for peer connection cleanup?
            }
#endif
        }

#if NETFX_CORE
        public override async void _play(JObject parameters) {
            Messenger.Broadcast(SympleLog.LogTrace, "symple:webrtc: _play");

            // if there is an active stream, play it now
            if (this.activeStream != null)
            {
                Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: active stream is not null, shuld play it now (TODO)");
                //this.video.src = URL.createObjectURL(this.activeStream);
                //this.video.play();
                this.setState("playing");
            } else
            {
                // otherwise, wait until ICE to complete before setting the playing state

                // if we are the ICE initiator, then attempt to open the local video device and send the SDP offer to the peer
                if (this.initiator)
                {
                    Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: initiating");

                    var videoCaptureDevices = GetMedia().GetVideoCaptureDevices();

                    Messenger.Broadcast(SympleLog.LogInfo, "videoCaptureDevices:");
                    foreach (var dev in videoCaptureDevices)
                    {
                        Messenger.Broadcast(SympleLog.LogInfo, "id = " + dev.Id + ", name = " + dev.Name + ", location = " + dev.Location);
                        var capabilities = await dev.GetVideoCaptureCapabilities();
                        foreach (var capability in capabilities)
                        {
                            Messenger.Broadcast(SympleLog.LogInfo, "\t" + capability.FullDescription);
                        }
                    }

                    var videoDevice = videoCaptureDevices[0];

                    Messenger.Broadcast(SympleLog.LogDebug, "getting videoCaptureCapabilities");
                    var videoCaptureCapabilities = await videoDevice.GetVideoCaptureCapabilities();
                    Messenger.Broadcast(SympleLog.LogDebug, "got videoCaptureCapabilities");

                    GetMedia().SelectVideoDevice(videoCaptureDevices[0]);

                    
                    // We need to specify a preferred video capture format; it has to be one of the supported capabilities of the device.
                    // We will choose the capability that has the lowest resolution and the highest frame rate for that resolution.
                    var chosenCapability = videoCaptureCapabilities[0];
                    foreach (var capability in videoCaptureCapabilities)
                    {
                        if (capability.Width == 640 && capability.Height == 480)
                        {
                            // we'd prefer to just do 640x480 if possible
                            chosenCapability = capability;
                            break;
                        }

                        if ( (capability.Width < chosenCapability.Width && capability.Height < chosenCapability.Height) ||
                            (capability.Width == chosenCapability.Width && capability.Height == chosenCapability.Height && capability.FrameRate > chosenCapability.FrameRate) )
                        {
                            chosenCapability = capability;
                        }
                    }
                    
                    Messenger.Broadcast(SympleLog.LogDebug, "chosenCapability:");
                    Messenger.Broadcast(SympleLog.LogDebug, "\tWidth: " + (int)chosenCapability.Width);
                    Messenger.Broadcast(SympleLog.LogDebug, "\tHeight: " + (int)chosenCapability.Height);
                    Messenger.Broadcast(SympleLog.LogDebug, "\tFrameRate: " + (int)chosenCapability.FrameRate);
                    WebRTC.SetPreferredVideoCaptureFormat((int)chosenCapability.Width, (int)chosenCapability.Height, (int)chosenCapability.FrameRate);

                    //WebRTC.SetPreferredVideoCaptureFormat(640, 480, 30);

                    //Org.WebRtc.Media.SetDisplayOrientation(Windows.Graphics.Display.DisplayOrientations.None);

                    Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: before getUserMedia");
                    if (_localStream == null)
                    {
                        _localStream = await GetMedia().GetUserMedia(new RTCMediaStreamConstraints { videoEnabled = true, audioEnabled = true });
                    }
                    Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: after getUserMedia");

                    // play the local video stream and create the SDP offer
                    this.pc.AddStream(_localStream);

                    Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: should play the local stream and create the SDP offer (TODO)");

                    Messenger.Broadcast(SympleLog.LogInfo, "localStream: " + _localStream);
                    var videoTracks = _localStream.GetVideoTracks();
                    Messenger.Broadcast(SympleLog.LogInfo, "videoTracks in localStream: ");
                    foreach (var track in videoTracks)
                    {
                        Messenger.Broadcast(SympleLog.LogInfo, track.Id + ", enabled = " + track.Enabled + ", kind = " + track.Kind + ", suspended = " + track.Suspended);
                    }
                    var audioTracks = _localStream.GetAudioTracks();
                    Messenger.Broadcast(SympleLog.LogInfo, "audioTracks in localStream: ");
                    foreach (var track in audioTracks)
                    {
                        Messenger.Broadcast(SympleLog.LogInfo, track.Id + ", enabled = " + track.Enabled + ", kind = " + track.Kind);
                    }

                    if (videoTracks.Count > 0)
                    {
                        var source = GetMedia().CreateMediaSource(videoTracks[0], Symple.LocalMediaStreamId);
                        
                        Messenger.Broadcast(SympleLog.CreatedMediaSource, source);

                        if (this.pc != null)
                        {
                            RTCSessionDescription desc = await this.pc.CreateOffer();

                            Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: offer: " + desc);
                            this._onLocalSDP(desc);

                            // store the active local stream
                            this.activeStream = _localStream;
                        } else
                        {
                            Messenger.Broadcast(SympleLog.LogError, "peer connection was destroyed while trying to creat offer");
                        }
                        
                    } else
                    {
                        Messenger.Broadcast(SympleLog.LogError, "ERROR: No video track found locally");
                    }
                    
                }
            }
        }
        
        // called when local SDP is ready to be sent to the peer
        private async void _onLocalSDP(RTCSessionDescription desc) {
            try
            {
                Messenger.Broadcast(SympleLog.LogTrace, "symple:webrtc: _onLocalSDP");

                string localSdp = desc.Sdp;
                Messenger.Broadcast(SympleLog.LogTrace, "symple:webrtc: localSdp = " + localSdp);

                List<int> localVideoCodecIds = Symple.GetVideoCodecIds(localSdp);
                string logMsg = "localVideoCodecIds: ";
                foreach (var videoCodecId in localVideoCodecIds)
                {
                    logMsg += videoCodecId + " ";
                }
                Messenger.Broadcast(SympleLog.LogTrace, "symple:webrtc: " + logMsg);


                await this.pc.SetLocalDescription(desc);
                this.sendLocalSDP(desc);
            } catch (Exception e)
            {
                Messenger.Broadcast(SympleLog.LogError, "symple:webrtc: failed to send local SDP; " + e);
            }
        }


        // called when remote SDP is received from the peer
        public async void recvRemoteSDP(JObject desc)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: recv remote sdp " + desc);
            if (desc == null || desc["type"] == null || desc["sdp"] == null)
            {
                throw new Exception("invalid remote SDP");
            }

            try
            {
                string sdpTypeString = (string) desc["type"];
                RTCSdpType sdpType;

                if (sdpTypeString.Equals("offer"))
                {
                    sdpType = RTCSdpType.Offer;
                }
                else if (sdpTypeString.Equals("pranswer"))
                {
                    sdpType = RTCSdpType.Pranswer;
                }
                else if (sdpTypeString.Equals("answer"))
                {
                    sdpType = RTCSdpType.Answer;
                } else {
                    throw new Exception("unknown rtc sdp type: " + sdpTypeString);
                }

                string sdp = (string)desc["sdp"];

                Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: before setRemoteDescription");
                await this.pc.SetRemoteDescription(new RTCSessionDescription(sdpType, sdp));
                Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: after setRemoteDescription");

                Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: sdp success");

                if (sdpType == RTCSdpType.Offer)
                {
                    var answer = await this.pc.CreateAnswer();
                    // assume success:
                    this._onLocalSDP(answer);
                }

            } catch (Exception e)
            {
                Messenger.Broadcast(SympleLog.LogError, "symple:webrtc: sdp error: " + e);
                this.setError("cannot parse remote sdp offer");
            }
        }

        // called when remote candidate is received from the peer
        public async void recvRemoteCandidate(JObject candidateParams)
        {
            Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: recv remote candidate " + candidateParams);
            if (this.pc == null)
            {
                throw new Exception("the peer connection is not initialized"); // call recvRemoteSDP first
            }

            string candidate = (string) candidateParams["candidate"];
            string sdpMid = (string) candidateParams["sdpMid"];
            ushort sdpMLineIndex = (ushort) candidateParams["sdpMLineIndex"];
            
            await this.pc.AddIceCandidate(new RTCIceCandidate(candidate, sdpMid, sdpMLineIndex));
        }

        // Called when local SDP is ready to be sent to the peer.
        public Action<RTCSessionDescription> sendLocalSDP = null; // new Function,

        // Called when a local candidate is ready to be sent to the peer.
        public Action<RTCIceCandidate> sendLocalCandidate = null; // new Function,

        private void _createPeerConnection()
        {
            if (this.pc != null)
            {
                throw new Exception("the peer connection is already initialized");
            }

            Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: create peer connection: " + this.rtcConfig);

            this.pc = new RTCPeerConnection(this.rtcConfig);
            Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: created this.pc");
            pc.OnIceCandidate += (RTCPeerConnectionIceEvent iceEvent) =>
            {
                if (iceEvent.Candidate != null)
                {
                    Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: candidate gathered: " + iceEvent.Candidate);
                    if (sendLocalCandidate != null)
                    {
                        this.sendLocalCandidate(iceEvent.Candidate);
                    }
                } else
                {
                    Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: candidate gathering complete");
                }
            };

            pc.OnAddStream += (MediaStreamEvent mediaStreamEvent) =>
            {
                //string objectURL = createObjectURL(mediaStreamEvent.Stream);
                Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: remote stream added");

                // Set the state to playing once candidates have completed gathering.
                // This is the best we can do until ICE onstatechange is implemented.
                this.setState("playing");

                // ====== here we would play the video element ======
                Messenger.Broadcast(SympleLog.LogDebug, "symple:webrtc: remote stream added, should play it now (TODO)");

                MediaVideoTrack peerVideoTrack = mediaStreamEvent.Stream.GetVideoTracks().FirstOrDefault();
                if (peerVideoTrack != null)
                {
                    IMediaSource mediaSource = GetMedia().CreateMediaSource(peerVideoTrack, RemotePeerVideoTrackId);
                    Messenger.Broadcast(SympleLog.LogInfo, "Created video source for remote peer video");
                    Messenger.Broadcast(SympleLog.CreatedMediaSource, mediaSource);
                } else
                {
                    Messenger.Broadcast(SympleLog.LogError, "ERROR: Received remote media stream, but there was no video track");
                }

                // store the active stream
                this.activeStream = mediaStreamEvent.Stream;
            };

            pc.OnRemoveStream += (MediaStreamEvent mediaStreamEvent) =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: remote stream removed: " + mediaStreamEvent);

                //this.video.stop();
                //this.video.src = "";
            };

            // NOTE: The following state events are still very unreliable.
            // Hopefully when the spec is complete this will change, but until then
            // we need to 'guess' the state.
            // this.pc.onconnecting = function(event) { Symple.log('symple:webrtc: onconnecting:', event); };
            // this.pc.onopen = function(event) { Symple.log('symple:webrtc: onopen:', event); };
            // this.pc.onicechange = function(event) { Symple.log('symple:webrtc: onicechange :', event); };
            // this.pc.onstatechange = function(event) { Symple.log('symple:webrtc: onstatechange :', event); };
        }
#endif

        public override void stop()
        {
            // note: stopping the player does not close the connection.
            // only "destroy" does that. this enables us to resume playback
            // quickly and with minimal delay.

            /*
            if (this.video)
            {
                this.video.src = "";
                // do not nullify
            }
            */

            this.setState("stopped");
        }

        public override void mute(bool flag)
        {
            Messenger.Broadcast(SympleLog.LogInfo, "symple:webrtc: mute " + flag);

            /*
            if (this.video)
            {
                this.video.prop("muted", flag);
            }
            */
        }
    }
}
