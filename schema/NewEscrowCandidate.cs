using Microsoft.AdCenter.BI.UET.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{
    public class NewEscrowCandidate:IStringSerialize
    {
        public Guid UETUserId;
        public Guid UAIPId;
        public Guid? AnalyticsGuid;
        public int TagId;
        public string TagName;
        [JsonConverter(typeof(BondConvert))]
        public EscrowVisitRecord EscrowRow;

        public NewEscrowCandidate(VisitsWithConversion data)
        {
            this.UETUserId = (Guid)data.UETUserId;
            this.UAIPId = data.UAIPId;
            this.AnalyticsGuid = data.AnalyticsGuid;
            this.TagId = data.TagId;
            this.TagName = data.TagName;
        }

        public NewEscrowCandidate() { }
    }
}
