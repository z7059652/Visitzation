using System;
using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.Bond;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{
    [Serializable]
    public class AppInstallVisitsSchema
    {
        public string AppInstallClickId;
        public Visit Visit;


        public static string Serialize ( AppInstallVisitsSchema schema)
        {
            return JsonConvert.SerializeObject(schema, Formatting.Indented);
        }

        public static AppInstallVisitsSchema Deserialize(string obj)
        {
            return (AppInstallVisitsSchema)JsonConvert.DeserializeObject(obj);
        }

        public AppInstallVisitsSchema ()
        {

        }
        public AppInstallVisitsSchema(SerializationInfo info, StreamingContext context)
        {
            string value = info.GetString("info");
            AppInstallVisitsSchema v = Deserialize(value);
            this.AppInstallClickId = v.AppInstallClickId;
            this.Visit = v.Visit;
        }
    }
}
