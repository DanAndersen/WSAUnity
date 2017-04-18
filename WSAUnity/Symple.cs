using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WSAUnity
{
    static class Symple
    {
        public static string buildAddress(JObject peer)
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

        // recursively merge object properties of r into l
        public static JObject merge(JObject l, JObject r)
        {
            foreach (var prop in r.Properties())
            {
                string p = prop.Name;

                try
                {
                    // property in destination object set; update its value.
                    if (r[p].Type == JTokenType.Object)
                    {
                        JObject lpObj = (JObject)l[p];
                        JObject rpObj = (JObject)r[p];
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

        public static JObject parseAddress(string str)
        {
            JObject addr = new JObject();

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
        public static bool match (JObject l, JObject r)
        {
            bool res = true;
            foreach (var prop in l.Properties())
            {
                string p = prop.Name;

                if (l[p] == null || r[p] == null || r[p] != l[p])
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        public static JObject extend(JObject destination, JObject source)
        {
            var result = destination;
            foreach (var sourceProperty in source.Properties())
            {
                destination[sourceProperty.Name] = source[sourceProperty.Name];
            }
            return result;
        }
    }
}
