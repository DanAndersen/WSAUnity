﻿using System;
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

        // used when a message of unknown format is received from another peer. Could be, for example, an annotation command.
        public const string IncomingMessage = "incoming_message";

        public const string RemoteAnnotationReceiverConnected = "remote_annotation_receiver_connected";
        public const string RemoteAnnotationReceiverDisconnected = "remote_annotation_receiver_disconnected";

        // triggered whenever some client connects to or disconnects from the signalling server. The username (e.g. "star-trainee") is passed as a parameter.
        public const string PeerAdded = "peer_added";
        public const string PeerRemoved = "peer_removed";
    }
}
