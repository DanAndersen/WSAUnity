using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

#if NETFX_CORE
using System.Threading.Tasks;
using Org.WebRtc;
#endif

namespace WSAUnity
{
    public class SymplePlayerEngineWebRTC : SymplePlayerEngine
    {

        bool initiator;

#if NETFX_CORE
        private RTCConfiguration rtcConfig;
        private RTCPeerConnection pc;
        private MediaStream activeStream;
        private Media _media;
        private RTCMediaStreamConstraints userMediaConstraints;
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
            Debug.WriteLine("symple:webrtc: init");

#if NETFX_CORE
            rtcConfig = player.options.rtcConfig ?? new RTCConfiguration() { IceServers = { new RTCIceServer() { Url = "stun:stun.l.google.com:19302" } } };
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

#if NETFX_CORE
            // The `MediaStreamConstraints` object to pass to `getUserMedia`
            this.userMediaConstraints = player.options.userMediaConstraints ?? new RTCMediaStreamConstraints() { audioEnabled = true, videoEnabled = true };

            // Reference to the active local or remote media stream
            this.activeStream = null;
#endif
        }

        public override void setup()
        {
            Debug.WriteLine("symple:webrtc: setup");

#if NETFX_CORE
            Debug.WriteLine("before _createPeerConnection");
            this._createPeerConnection();
            Debug.WriteLine("after _createPeerConnection");
#endif

            Debug.WriteLine("====== here is where we could create the 'video' element and add it to the webpage ======");
        }

        public override void destroy()
        {
            Debug.WriteLine("symple:webrtc: destroy");

#if NETFX_CORE
            this.sendLocalSDP = null;
            this.sendLocalCandidate = null;
            this.activeStream = null; // TODO: needs explicit close?
#endif

            Debug.WriteLine("====== here is where we would destroy the video element ======");
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
            Debug.WriteLine("symple:webrtc: _play");

            // if there is an active stream, play it now
            if (this.activeStream != null)
            {
                Debug.WriteLine("====== here we would play the video element ======");
                //this.video.src = URL.createObjectURL(this.activeStream);
                //this.video.play();
                this.setState("playing");
            } else
            {
                // otherwise, wait until ICE to complete before setting the playing state

                // if we are the ICE initiator, then attempt to open the local video device and send the SDP offer to the peer
                if (this.initiator)
                {
                    Debug.WriteLine("symple:webrtc: initiating " + this.userMediaConstraints);

                    _media = Media.CreateMedia();
                    
                    MediaStream localStream = await _media.GetUserMedia(this.userMediaConstraints);

                    // play the local video stream and create the SDP offer

                    Debug.WriteLine("====== this.video.src = URL.createObjectURL(localStream); ======");
                    
                    this.pc.AddStream(localStream);
                    RTCSessionDescription desc = await this.pc.CreateOffer();

                    Debug.WriteLine("symple:webrtc: offer: " + desc);
                    this._onLocalSDP(desc);

                    // store the active local stream
                    this.activeStream = localStream;
                }
            }

            throw new NotImplementedException();
        }
        
        // called when local SDP is ready to be sent to the peer
        private async void _onLocalSDP(RTCSessionDescription desc) {
            try
            {
                await this.pc.SetLocalDescription(desc);
                this.sendLocalSDP(desc);
            } catch (Exception e)
            {
                Debug.WriteLine("symple:webrtc: failed to send local SDP; " + e);
            }
        }


        // called when remote SDP is received from the peer
        public async void recvRemoteSDP(JObject desc)
        {
            Debug.WriteLine("symple:webrtc: recv remote sdp " + desc);
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

                await this.pc.SetRemoteDescription(new RTCSessionDescription(sdpType, sdp));
                Debug.WriteLine("symple:webrtc: sdp success");
            } catch (Exception e)
            {
                Debug.WriteLine("symple:webrtc: sdp error: " + e);
                this.setError("cannot parse remote sdp offer");
            }
        }

        // called when remote candidate is received from the peer
        public async void recvRemoteCandidate(JObject candidateParams)
        {
            Debug.WriteLine("symple:webrtc: recv remote candidate " + candidateParams);
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

            Debug.WriteLine("symple:webrtc: create peer connection: " + this.rtcConfig); // NOTE: removed rtcOptions

            this.pc = new RTCPeerConnection(this.rtcConfig);
            Debug.WriteLine("symple:webrtc: created this.pc");
            pc.OnIceCandidate += (RTCPeerConnectionIceEvent iceEvent) =>
            {
                if (iceEvent.Candidate != null)
                {
                    Debug.WriteLine("symple:webrtc: candidate gathered: " + iceEvent.Candidate);
                    if (sendLocalCandidate != null)
                    {
                        this.sendLocalCandidate(iceEvent.Candidate);
                    }
                } else
                {
                    Debug.WriteLine("symple:webrtc: candidate gathering complete");
                }
            };

            pc.OnAddStream += (MediaStreamEvent mediaStreamEvent) =>
            {
                //string objectURL = createObjectURL(mediaStreamEvent.Stream);
                Debug.WriteLine("symple:webrtc: remote stream added");

                // Set the state to playing once candidates have completed gathering.
                // This is the best we can do until ICE onstatechange is implemented.
                this.setState("playing");

                Debug.WriteLine("====== here we would play the video element ======");
                //this.video.src = objectURL;
                //this.video.play();

                // store the active stream
                this.activeStream = mediaStreamEvent.Stream;
            };

            pc.OnRemoveStream += (MediaStreamEvent mediaStreamEvent) =>
            {
                Debug.WriteLine("symple:webrtc: remote stream removed: " + mediaStreamEvent);

                Debug.WriteLine("====== here we would stop the video element ======");
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

            Debug.WriteLine("====== here we would stop the video element ======");
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
            Debug.WriteLine("symple:webrtc: mute " + flag);

            Debug.WriteLine("====== here we would mute the video element ======");
            /*
            if (this.video)
            {
                this.video.prop("muted", flag);
            }
            */
        }
    }
}
