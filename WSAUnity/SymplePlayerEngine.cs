using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public virtual async void play(var parameters)
        {
            throw new NotImplementedException();
        }

        public abstract void stop();
        public abstract void pause(var flag);
        public abstract void mute(var flag);

        public virtual void setState(var state, var message)
        {
            this.player.setState(state, message);
        }

        public virtual void setError(var error)
        {
            Debug.WriteLine("symple:player:engine: error " + error);
            this.setState("error", error);
        }

        public virtual void onRemoteCandidate(var candidate)
        {
            Debug.WriteLine("symple:player:engine: remote candidates not supported");
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

        public Uri buildURL()
        {
            if (this.params == null) {
                throw new Exception("Streaming parameters not set");
            }

            if (this.params.address == null) {
                this.params.address = this.player.options.address;
            }

            return SympleMedia.buildURL(this.params);
        }
    }
}
