using System;
using System.Collections.Generic;
//using Microsoft.AdCenter.BI.UET.Common.Helpers;
using Microsoft.AdCenter.BI.UET.Schema;
//using Microsoft.BI.CFR;
using Microsoft.Bond;
using Newtonsoft.Json;
using System.IO;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{

    public class BondConvert : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IBondSerializable);
        }


        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return null;

            var payload = Convert.FromBase64String(reader.Value.ToString());
            IBondSerializable scd = (IBondSerializable)Activator.CreateInstance(objectType);
            using (var ms = new MemoryStream(payload))
            {
                using (var protocolReader = new CompactBinaryProtocolReader(ms))
                {
                    scd.Read(protocolReader);
                }
            }
            return scd;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CompactBinaryProtocolWriter compactBinaryProtocolWriter = new CompactBinaryProtocolWriter(memoryStream))
                {
                    ((IBondSerializable)value).Write(compactBinaryProtocolWriter);
                    result = memoryStream.ToArray();
                }
            }
            string str = Convert.ToBase64String(result);
            writer.WriteValue(str);
        }
    }
}
