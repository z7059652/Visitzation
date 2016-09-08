using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AdCenter.BI.UET.Schema;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{
    public class VisitizationSchema
    {
        public Guid UETUserId;
        public Guid UAIPId;
        public Guid? AnalyticsGuid;
        public int TagId;
        public string TagName;
        [JsonConverter(typeof(BondConvert))]
        public SAEventConversionFacts SAEventConversionFactsRow;

        public VisitizationSchema()
        {

        }

        public static string Serialize(VisitizationSchema schema)
        {
            return JsonConvert.SerializeObject(schema);
        }

        public static VisitizationSchema Deserialize(string value)
        {
            return (VisitizationSchema) JsonConvert.DeserializeObject(value);
        }

    }

    public class VisitsForUser_WithTypeOfUser
    {
        public Guid? ANID = null;
        public Guid? MUID = null;
        public short TypeOfUser = -1;
        public Guid UAIPId;
        public Guid? AnalyticsGuid;
        public int TagId;
        public string TagName;
        [JsonConverter(typeof(BondConvert))]
        public SAEventConversionFacts SAEventConversionFactsRow;

        public VisitsForUser_WithTypeOfUser()
        {

        }
        public static string Serialize(VisitsForUser_WithTypeOfUser schema)
        {
            return JsonConvert.SerializeObject(schema);
        }

        public static VisitsForUser_WithTypeOfUser Deserialize(string value)
        {
            return (VisitsForUser_WithTypeOfUser)JsonConvert.DeserializeObject(value);
        }
    }
}
