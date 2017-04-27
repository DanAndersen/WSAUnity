using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if NETFX_CORE
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
#endif

using System.Diagnostics;

namespace WSAUnity
{
    static class Symple
    {
#if NETFX_CORE
        public static string buildAddress(JObject peer)
        {
            return (peer["user"] != null ? (peer["user"] + "|") : "") + (peer["id"] != null ? peer["id"] : "");
        }
#endif

        public const string LocalMediaStreamId = "LOCAL";


        private static Random random = new Random();

        // generate a random string
        // NOTE: here we are actually creating a string of a particular length; in the current symple codebase, it is not enforcing the same character limit.
        public static string randomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

#if NETFX_CORE
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
                    }
                    else
                    {
                        l[p] = r[p];
                    }
                }
                catch (Exception e)
                {
                    // property in destination object not set;
                    // create it and set its value.
                    l[p] = r[p];
                }
            }
            return l;
        }
#endif

#if NETFX_CORE
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
#endif

#if NETFX_CORE
        // match object properties of l with r
        public static bool match(JObject l, JObject r)
        {
            bool res = true;
            foreach (var prop in l.Properties())
            {
                string p = prop.Name;

                if (l[p] == null || r[p] == null || !r[p].Equals(l[p]))
                {
                    res = false;
                    break;
                }
            }
            return res;
        }
#endif

#if NETFX_CORE
        public static JObject extend(JObject destination, JObject source)
        {
            var result = destination;
            foreach (var sourceProperty in source.Properties())
            {
                destination[sourceProperty.Name] = source[sourceProperty.Name];
            }
            return result;
        }
#endif


        public static List<int> GetVideoCodecIds(string sdp)
        {
            var mfdRegex = new Regex("\r\nm=video.*RTP.*?( .\\d*)+\r\n");
            var mfdMatch = mfdRegex.Match(sdp);
            var mfdList = new List<int>(); //mdf = media format descriptor
            var videoMediaDescFound = mfdMatch.Groups.Count > 1; //Group 0 is whole match
            if (videoMediaDescFound)
            {
                for (var groupCtr = 1 /*Group 0 is whole match*/; groupCtr < mfdMatch.Groups.Count; groupCtr++)
                {
                    for (var captureCtr = 0; captureCtr < mfdMatch.Groups[groupCtr].Captures.Count; captureCtr++)
                    {
                        string codecId = mfdMatch.Groups[groupCtr].Captures[captureCtr].Value.TrimStart();
                        mfdList.Add(int.Parse(codecId));
                    }
                }
            }
            return mfdList;
        }
    }
}
