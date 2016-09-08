using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SerializaType = System.Byte;
using Microsoft.Bond;
using System.IO;

namespace Microsoft.AdCenter.BI.UET.StreamingSchema
{

    [Serializable()]
    public class IStringSerialize
    {
        public IStringSerialize()
        {
        }
        public virtual string SerializeObject()
        {
            return JsonConvert.SerializeObject(this);
        }

        public virtual IStringSerialize DeserializeObject(string line)
        {
            Type types = this.GetType();
            return (IStringSerialize)JsonConvert.DeserializeObject(line, types);
        }
    }

    [Serializable()]
    public static class IExtend
    {
        public static T DeserializeObject<T>(this string line) where T : IStringSerialize, new()
        {
            T a = new T();
            return (T)a.DeserializeObject(line);
        }

        public static T DeserializeBondObject<T>(this byte[] line) where T : IBondSerializable, new()
        {
            T a = new T();
            var payload = line;
            using (var ms = new MemoryStream(payload))
            {
                using (var protocolReader = new CompactBinaryProtocolReader(ms))
                {
                    a.Read(protocolReader);
                }
            }
            return a;
        }
        public static T DeserializeBondObject<T>(this string line) where T : IBondSerializable, new()
        {
            T a = new T();
            var payload = Convert.FromBase64String(line);
            using (var ms = new MemoryStream(payload))
            {
                using (var protocolReader = new CompactBinaryProtocolReader(ms))
                {
                    a.Read(protocolReader);
                }
            }
            return a;
        }
        public static SerializaType[] SerializeBondObject(this IBondSerializable value)
        {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CompactBinaryProtocolWriter compactBinaryProtocolWriter = new CompactBinaryProtocolWriter(memoryStream))
                {
                    ((IBondSerializable)value).Write(compactBinaryProtocolWriter);
                    result = memoryStream.ToArray();
                }
            }
            return result;
            //string str = Convert.ToBase64String(result);
            //return str;
        }
    }
}
