using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.Bond;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{
    [Serializable]
    public class UETLogView
    {
        public string DedupKey;
        public long EventDateTime;
        public Guid? ANID;
        public Guid? MUID;
        public bool? IsNewMUID;
        public Guid UAIPId;

        [JsonConverter(typeof(BondConvert))]
        public ClientIPData ClientIP;

        public string IP;
        public string UserAgent;
        public string ReferrerURL;
        public string LogServerName;
        public string PageTitle;

        [JsonConverter(typeof(BondConvert))]
        public CustomEvent customEvent;

        public int TagId;
        public string TagName;
        public string NavigatedFromURL;
        public double? GoalValue;
        public sbyte? PageLoad;
        public string Version;
        public string QueryString;
        public Guid? UETMatchingGuid;
        public string EventType;
        public string AppInstallClickId;
        public Guid? AnalyticsGuid;

        public static string Serialize(UETLogView schema)
        {
            return JsonConvert.SerializeObject(schema);
        }

        public static UETLogView Deserialize(string value)
        {
            return JsonConvert.DeserializeObject<UETLogView>(value);
        }

        public UETLogView()
        {

        }
    }
}
