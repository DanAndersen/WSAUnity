using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SymplePlayer
    {
        string state;

        bool playing;

        public SymplePlayerOptions options { get; }

        public SymplePlayerEngine engine { get; private set; }

        public SymplePlayer(SymplePlayerOptions options)
        {
            this.options = options;
            this.options.format = "MJPEG";
            this.options.engine = null;
            this.options.onCommand = (player, cmd) => { };
            this.options.onStateChange = (player, state, message) => { };

            if (this.options.engine == null)
            {
                var engine = SympleMediaPreferredCompatibleEngine(this.options.format);
                if (engine != null)
                {
                    this.options.engine = engine.id;
                }
            }
            
            this.bindEvents();
            this.playing = false;
        }

        private void onStateChange(string state)
        {
            this.displayStatus(state);
        }

        public void play(Dictionary<string,object> parameters)
        {
            Debug.WriteLine("symple:player: play, " + parameters);
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
                this.displayMessage("error", e);
                throw e;
            }
        }

        private void mute(bool flag)
        {
            Debug.WriteLine("symple:player: mute " + flag);

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
                Debug.WriteLine(data);
            } else
            {
                Debug.WriteLine("");
            }
        }

        public void setState(string state, string message = null)
        {
            Debug.WriteLine("symple:player: set state " + this.state + " => " + state);

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

            Debug.WriteLine("TODO: change any appearances in the UI based on state change");

            this.options.onStateChange(this, state, message);
        }

        void displayMessage(string type = null, string message = null)
        {
            Debug.WriteLine("symple:player: display message " + type + " " + message);
        }
    }
}
