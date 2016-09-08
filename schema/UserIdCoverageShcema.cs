using System;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{

    [Serializable]
    public class UserIdCoverageShcema
    {
        public Guid? UETMatchingGuid; // ----Is it right to set UETMathchingGuid as Guid?, since it's the key to join with uetlog to get AnalyticsGuid?
        public Guid? AnalyticsGuid;

        public UserIdCoverageShcema()
        {

        }
        public static UserIdCoverageShcema Deserialize(string value)
        {
            return JsonConvert.DeserializeObject<UserIdCoverageShcema>(value);
        }
        public static string Serialize(UserIdCoverageShcema schema)
        {
            return JsonConvert.SerializeObject(schema, Formatting.Indented);
        }
    }
}
