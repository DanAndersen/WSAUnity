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
    class SymplePlayer
    {
        string _format;
        string _engine;

        public SymplePlayer()
        {
            this._format = "MJPEG";
            this._engine = "WebRTC";

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
    }
}
