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
            Debug.WriteLine("debug.writeline");

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
            var _media = Media.CreateMedia();
            Debug.WriteLine("_media:");
            Debug.WriteLine(_media);

            var acd = _media.GetAudioCaptureDevices();
            Debug.WriteLine("acd size: " + acd.Count);

            var apd = _media.GetAudioPlayoutDevices();
            Debug.WriteLine("apd size: " + apd.Count);

            var vcd = _media.GetVideoCaptureDevices();
            Debug.WriteLine("vcd size: " + vcd.Count);

            MediaStream _localStream = await _media.GetUserMedia(new RTCMediaStreamConstraints() { audioEnabled = true, videoEnabled = true });
            Debug.WriteLine("_localStream: " + _localStream);
        }
#endif
    }
}
