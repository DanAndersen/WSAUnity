using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSAUnity
{
    public class SymplePlayerOptions
    {
        public string format;
        public string engine;
        public Action<SymplePlayer, string> onCommand;
        public Action<SymplePlayer, string> onStateChange;
    }
}
