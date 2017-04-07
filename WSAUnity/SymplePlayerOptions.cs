using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Org.WebRtc;

namespace WSAUnity
{
    public class SymplePlayerOptions
    {
        public string format;
        public string engine;
        public Action<SymplePlayer, string> onCommand;
        public Action<SymplePlayer, string, string> onStateChange;
        public RTCConfiguration rtcConfig;
        public bool initiator;
        public RTCMediaStreamConstraints userMediaConstraints;
    }
}
