//using Microsoft.AdCenter.BI.UET.Schema;
//using Microsoft.Bond;
//using Newtonsoft.Json;
//using System;
//using System.IO;
//using System.Runtime.Serialization;

//namespace Microsoft.AdCenter.BI.UET.StreamingSchema
//{
//    [Serializable]
//    public class UMS_MUID : IStringSerialize
//    {
//        public Guid MUID;
//        public Guid StableIdValue;
//        public UETUserIdType StableIdType;

//        public UMS_MUID(string line)
//        {
//            string[] values = line.Split('\t');
//            this.MUID = Guid.Parse(values[0]);
//            this.StableIdValue = Guid.Parse(values[1]);
//            this.StableIdType = Utilities.GetUETUserIdTypeFromStableIdType(values[2]);
//        }
//        public UMS_MUID() { }

//        public UMS_MUIDSpark Convert2Spark()
//        {
//            var umsSpark = new UMS_MUIDSpark();
//            umsSpark.MUID = new Bond.GUID(this.MUID);
//            umsSpark.StableIdValue = new Bond.GUID(this.StableIdValue);
//            umsSpark.StableIdType = (UETUserIdTypeSpark)((int)this.StableIdType);
//            return umsSpark;
//        }
//    }

//    public class UMS_ANID : IStringSerialize
//    {
//        public Guid ANID;
//        public Guid StableIdValue;
//        public UETUserIdType StableIdType;

//        public UMS_ANID(string line)
//        {
//            string[] values = line.Split('\t');
//            this.ANID = Guid.Parse(values[0]);
//            this.StableIdValue = Guid.Parse(values[1]);
//            this.StableIdType = Utilities.GetUETUserIdTypeFromStableIdType(values[2]);
//        }
//        public UMS_ANID() { }

//        public UMS_ANIDSpark Convert2Spark()
//        {
//            var umsSpark = new UMS_ANIDSpark();
//            umsSpark.ANID = new Bond.GUID(this.ANID);
//            umsSpark.StableIdValue = new Bond.GUID(this.StableIdValue);
//            umsSpark.StableIdType = (UETUserIdTypeSpark)((int)this.StableIdType);
//            return umsSpark;
//        }
//    }

//}
