using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

#if NETFX_CORE
using System.Threading.Tasks;
using Windows.Devices;
using Org.WebRtc;
#endif

namespace WSAUnity
{
    public class Plugin
    {
        string statusMsg;

        public string GetStatus()
        {
            statusMsg = "Plugin implemented successfully";
            return statusMsg;
        }

        public void foo()
        {
#if NETFX_CORE
            foo_private();
#endif
        }

#if NETFX_CORE
        private async void foo_private()
        {
            
        }
#endif
    }
}
