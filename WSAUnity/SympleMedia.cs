using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
#endif
using System.Diagnostics;

namespace WSAUnity
{
    public class SympleMedia
    {
        private static SympleMedia instance;

        private SympleMedia() { }

        public static SympleMedia Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SympleMedia();
                }
                return instance;
            }
        }

        Dictionary<string, SympleEngineOptions> engines = new Dictionary<string, SympleEngineOptions>();

        public bool registerEngine(SympleEngineOptions engine)
        {
            Messenger.Broadcast(SympleLog.LogTrace, "register media engine: " + engine);
            if (engine.name == null || engine.support == null)
            {
                Messenger.Broadcast(SympleLog.LogError, "symple:media: cannot register invalid engine" + engine);
                return false;
            }
            this.engines[engine.id] = engine;
            return true;
        }


        // NOTE: only doing WebRTC engine
        public SympleEngineOptions preferredCompatibleEngine(string format)
        {
            return this.engines["WebRTC"];
        }





    }
}
