using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    public class SympleLog
    {
        public const string LogTrace = "log_trace";
        public const string LogDebug = "log_debug";
        public const string LogInfo = "log_info";
        public const string LogError = "log_error";

        public const string Connected = "connected";
        public const string Announced = "announced";
        public const string Reconnecting = "reconnecting";
        public const string ConnectFailed = "connect_failed";
        public const string Disconnect = "disconnect";
        public const string StateChanged = "state_changed";

        public const string CreatedMediaSource = "created_media_source";

        public const string DestroyedMediaSource = "destroyed_media_source";
    }
}
