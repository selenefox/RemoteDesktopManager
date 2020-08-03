using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDesktopManager
{
    public class JSONUtils
    {
        public static AccountItem[] Parse(string jsonPath)
        {
            try
            {
                if (!File.Exists(jsonPath))
                    return new AccountItem[] { };

                string jsonContents = File.ReadAllText(jsonPath);
                return JsonConvert.DeserializeObject<AccountItem[]>(jsonContents);
            }
            catch
            {
                Trace.TraceError("Read config failed.");
                return null;
            }
        }

        public static void Write(AccountItem[] datas, string jsonPath)
        {
            try
            {
                using (StreamWriter w = new StreamWriter(jsonPath, false, Encoding.UTF8))
                {
                    string json = JsonConvert.SerializeObject(datas);
                    w.Write(json);
                }
            }
            catch
            {
                Trace.TraceError("Write config failed.");
            }
        }

        public static Dictionary<String, Object> ToMap(Object o)
        {
            Dictionary<String, Object> map = new Dictionary<string, object>();
            Type t = o.GetType();
            PropertyInfo[] pi = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo p in pi)
            {
                MethodInfo mi = p.GetGetMethod();
                if (mi != null && mi.IsPublic)
                {
                    map.Add(p.Name, mi.Invoke(o, new Object[] { }));
                }
            }
            return map;
        }
    }

    public class AccountItem
    {
        [JsonProperty("accountName")]
        public string accountName;
        [JsonProperty("address")]
        public string address;
        [JsonProperty("port")]
        public int port;
        [JsonProperty("loginname")]
        public string loginname;
        [JsonProperty("password")]
        public string password;
        [JsonProperty("useMultimon")]
        public bool useMultimon;
    }
}
