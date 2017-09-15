using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

#if NETFX_CORE
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SymplePlayer
    {
        static SymplePlayer()
        {
#if NETFX_CORE
            Messenger.Broadcast(SympleLog.LogTrace, "registering WebRTC engine");
            SympleMedia.Instance.registerEngine(new SympleEngineOptions() { id = "WebRTC", name = "WebRTC Player", formats = "VP9, VP4, H.264, Opus", preference = 100, support = () => {
                return true;
            } });
#endif
        }


        string state;

        bool playing;

        public SymplePlayerOptions options { get; }

        public SymplePlayerEngine engine { get; set; }

        public SymplePlayer(SymplePlayerOptions opts)
        {
            this.options = opts;
            this.options.format = "MJPEG";
            this.options.engine = null;
            this.options.onCommand = (player, cmd) => { };
            this.options.onStateChange = (player, state, message) => { };

            if (this.options.engine == null)
            {
                var engine = SympleMedia.Instance.preferredCompatibleEngine(this.options.format);
                if (engine != null)
                {
                    this.options.engine = engine.id;
                }
            }
            
            //this.bindEvents(); // here we would set up the event logic to bind UI buttons to actions like play/stop/mute/unmute/etc
            this.playing = false;
        }

        private void onStateChange(string state)
        {
            this.displayStatus(state);
        }

#if NETFX_CORE
        public void play(JObject parameters)
        {
            Messenger.Broadcast(SympleLog.LogInfo, "symple:player: play, " + parameters);
            try
            {
                if (this.engine == null)
                {
                    this.setup();
                }

                if (this.state != "playing")
                {
                    this.setState("loading");
                    this.engine.play(parameters); // engine updates state to playing
                }
            } catch (Exception e)
            {
                this.setState("error");
                this.displayMessage("error", e.Message);
                throw e;
            }
        }
#endif

        private void mute(bool flag)
        {
            Messenger.Broadcast(SympleLog.LogInfo, "symple:player: mute " + flag);

            if (this.engine != null)
            {
                this.engine.mute(flag);
            }

            // TODO: add anything about showing/hiding display based on mute state
        }

        private void setup()
        {
            // assume we are only doing WebRTC engine

            // instantiate the engine
            this.engine = new SymplePlayerEngineWebRTC(this);
            this.engine.setup();
        }

        public void displayStatus(string data)
        {
            if (data != null)
            {
                Messenger.Broadcast(SympleLog.StateChanged, data);
            } else
            {
                Messenger.Broadcast(SympleLog.StateChanged, "");
            }
        }

        public void setState(string state, string message = null)
        {
            Messenger.Broadcast(SympleLog.LogInfo, "symple:player: set state " + this.state + " => " + state);

            if (state.Equals(this.state))
            {
                return;
            }

            this.state = state;
            this.displayStatus(null);
            this.playing = (state == "playing");
            if (message != null)
            {
                this.displayMessage(state == "error" ? "error" : "info", message);
            } else
            {
                this.displayMessage(null);
            }

            Messenger.Broadcast(SympleLog.StateChanged, state);

            this.options.onStateChange(this, state, message);
        }

        void displayMessage(string type = null, string message = null)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "symple:player: display message " + type + " " + message);
        }
    }
}
