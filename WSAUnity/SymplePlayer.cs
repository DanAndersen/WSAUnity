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

        public SymplePlayer(SymplePlayerOptions options)
        {
            this.options = options;
            this.options.format = "MJPEG";
            this.options.engine = null;
            this.options.onCommand = (player, cmd) => { };
            this.options.onStateChange = (player, state) => { };

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

        private void displayStatus(string data)
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
    }
}
