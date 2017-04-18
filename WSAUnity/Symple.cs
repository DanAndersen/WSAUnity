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

        private static Random random = new Random();

        // generate a random string
        // NOTE: here we are actually creating a string of a particular length; in the current symple codebase, it is not enforcing the same character limit.
        public static string randomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static Dictionary<string, object> initPresence(Dictionary<string, object> json)
        {
            Dictionary<string, object> presence = new Dictionary<string, object>();
            foreach (var key in json.Keys)
            {
                presence[key] = json[key];
            }

            presence["type"] = "presence";

            return presence;
        }

        // recursively merge object properties of r into l
        public static Dictionary<string, object> merge(Dictionary<string, object> l, Dictionary<string, object> r)
        {
            foreach (string p in r.Keys)
            {
                try
                {
                    // property in destination object set; update its value.
                    if (r[p].GetType() == typeof(Dictionary<string, object>))
                    {
                        Dictionary<string, object> lpObj = (Dictionary<string, object>)l[p];
                        Dictionary<string, object> rpObj = (Dictionary<string, object>)r[p];
                        l[p] = merge(lpObj, rpObj);
                    } else
                    {
                        l[p] = r[p];
                    }
                } catch (Exception e)
                {
                    // property in destination object not set;
                    // create it and set its value.
                    l[p] = r[p];
                }
            }
            return l;
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
