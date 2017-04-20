using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETFX_CORE
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
#endif
using System.Diagnostics;


namespace WSAUnity
{
    public abstract class SymplePlayerEngine
    {
        SymplePlayer player;
        float fps;
        int seq;
        Stopwatch stopwatch = null;
        long prevTime;
        long delta;

        public SymplePlayerEngine(SymplePlayer player)
        {
            this.player = player;
            this.fps = 0;
            this.seq = 0;
        }

        public abstract bool support();
        public abstract void setup();
        public abstract void destroy();
#if NETFX_CORE
        public void play(JObject parameters)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "symple:player:engine: play");

            _play(parameters);
        }
#endif

#if NETFX_CORE
        public virtual async void _play(JObject parameters) { }
#endif

        public abstract void stop();
        public virtual void pause(bool flag) { }
        public abstract void mute(bool flag);
        
        public virtual void setState(string state, string message = null)
        {
            this.player.setState(state, message);
        }

        public virtual void setError(string error)
        {
            Messenger.Broadcast(SympleLog.LogError, "symple:player:engine: error " + error);
            this.setState("error", error);
        }
        
        public void updateFPS()
        {
            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
                prevTime = stopwatch.ElapsedMilliseconds;
            }
            if (this.seq > 0)
            {
                long nowTime = stopwatch.ElapsedMilliseconds;
                this.delta = this.prevTime >= 0 ? nowTime - this.prevTime : 0;
                this.fps = (1000.0f / this.delta);
                this.prevTime = nowTime;
            }
            this.seq++;
        }

        public void displayFPS()
        {
            this.updateFPS();
            this.player.displayStatus(this.delta + " ms (" + this.fps + " fps)");
        }
    }
}
