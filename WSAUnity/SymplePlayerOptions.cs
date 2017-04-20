using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
#endif

#if NETFX_CORE
using Org.WebRtc;
#endif

namespace WSAUnity
{
    public class SymplePlayerOptions
    {
        public string format;
        public string engine;
        public Action<SymplePlayer, string> onCommand;
        public Action<SymplePlayer, string, string> onStateChange;
        public bool initiator;
#if NETFX_CORE
        public RTCConfiguration rtcConfig;
        public RTCMediaStreamConstraints userMediaConstraints;
#endif
    }
}
