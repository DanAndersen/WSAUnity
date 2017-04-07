using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSAUnity
{
    static class Symple
    {
        public static string buildAddress(Dictionary<string, object> peer)
        {
            return (peer["user"] != null ? (peer["user"] + "|") : "") + (peer["id"] != null ? peer["id"] : "");
        }

        public static Dictionary<string, object> parseAddress(string str)
        {
            Dictionary<string, object> addr = new Dictionary<string, object>();

            string[] arr = str.Split('|');

            if (arr.Length > 0)
            {
                // no id
                addr["user"] = arr[0];
            }

            if (arr.Length > 1)
            {
                // has id
                addr["id"] = arr[1];
            }

            return addr;
        }

        // match object properties of l with r
        public static bool match (Dictionary<string, object> l, Dictionary<string, object> r)
        {
            bool res = true;
            foreach (var prop in l.Keys)
            {
                if (!l.ContainsKey(prop) || !r.ContainsKey(prop) || r[prop] != l[prop])
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        public static Dictionary<string, object> extend(Dictionary<string, object> destination, Dictionary<string, object> source)
        {
            var result = destination;
            foreach (var sourceKey in source.Keys)
            {
                destination[sourceKey] = source[sourceKey];
            }
            return result;
        }
    }
}
