using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

#if NETFX_CORE
using System.Threading.Tasks;
using Org.WebRtc;
#endif

namespace WSAUnity
{
    public class SymplePlayerEngineWebRTC : SymplePlayerEngine
    {
        bool initiator;

        private RTCConfiguration rtcConfig;
        private RTCPeerConnection pc;
        private MediaStream activeStream;

        public override void init(SymplePlayer player)
        {
            Debug.WriteLine("symple:webrtc: init");
            base.init(player);

            this.rtcConfig = player.options.rtcConfig || {
                iceServers: [
                    { url: "stun:stun.l.google.com:19302" }
                ]
            };

            this.rtcOptions = player.options.rtcOptions || {
                optional: [
                    { DtlsSrtpKeyAgreement: true } // required for FF <=> Chrome interop
                ]
            };

            // Specifies that this client will be the ICE initiator,
            // and will be sending the initial SDP Offer.
            this.initiator = player.options.initiator;

            // The `MediaStreamConstraints` object to pass to `getUserMedia`
            this.userMediaConstraints = player.options.userMediaConstraints || {
                audio: true, 
                video: true
            };

            // Reference to the active local or remote media stream
            this.activeStream = null;
        }

        public override void setup()
        {
            Debug.WriteLine("symple:webrtc: setup");

            this._createPeerConnection();

            if (this.video == null)
            {
                // TODO: add the "video element" to the document
                throw new NotImplementedException();
            }
        }

        public override void destroy()
        {
            Debug.WriteLine("symple:webrtc: destroy");

            this.sendLocalSDP = null;
            this.sendLocalCandidate = null;
            this.activeStream = null; // TODO: needs explicit close?

            if (this.video != null)
            {
                this.video.src = '';
                this.video = null;
                // anything else needed for video cleanup?
            }

            if (this.pc != null)
            {
                this.pc.close();
                this.pc = null;
                // anything else needed for peer connection cleanup?
            }
        }

        public override void play(var params) {
            throw new NotImplementedException();
        }



        // Called when local SDP is ready to be sent to the peer.
        private Action<desc> sendLocalSDP = null; // new Function,

        // Called when a local candidate is ready to be sent to the peer.
        private Action<RTCIceCandidate> sendLocalCandidate = null; // new Function,

        private void _createPeerConnection()
        {
            if (this.pc != null)
            {
                throw new Exception("the peer connection is already initialized");
            }

            Debug.WriteLine("symple:webrtc: create peer connection: " + this.rtcConfig + " " + this.rtcOptions);

            this.pc = new RTCPeerConnection(this.rtcConfig);
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
                string objectURL = createObjectURL(mediaStreamEvent.Stream);
                Debug.WriteLine("symple:webrtc: remote stream added: " + objectURL);

                // Set the state to playing once candidates have completed gathering.
                // This is the best we can do until ICE onstatechange is implemented.
                this.setState("playing");

                this.video.src = objectURL;
                this.video.play();

                // store the active stream
                this.activeStream = mediaStreamEvent.Stream;
            };

            pc.OnRemoveStream += (MediaStreamEvent mediaStreamEvent) =>
            {
                Debug.WriteLine("symple:webrtc: remote stream removed: " + mediaStreamEvent);

                this.video.stop();
                this.video.src = "";
            };

            // NOTE: The following state events are still very unreliable.
            // Hopefully when the spec is complete this will change, but until then
            // we need to 'guess' the state.
            // this.pc.onconnecting = function(event) { Symple.log('symple:webrtc: onconnecting:', event); };
            // this.pc.onopen = function(event) { Symple.log('symple:webrtc: onopen:', event); };
            // this.pc.onicechange = function(event) { Symple.log('symple:webrtc: onicechange :', event); };
            // this.pc.onstatechange = function(event) { Symple.log('symple:webrtc: onstatechange :', event); };
        }
    }
}
