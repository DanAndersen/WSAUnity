using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace WSAUnity
{
    class SympleClient
    {
        string _url;
        bool _secure;

        public SympleClient(string url, bool secure)
        {
            _url = url;
            _secure = secure;
        }

        public void connect()
        {
            Debug.WriteLine("symple:client: connecting");


        }
    }
}
