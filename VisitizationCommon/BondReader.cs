using Microsoft.Bond;
using System;
using System.IO;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    // This class deserializes a byte array which is serialized using CompactBinaryProtocolWriter to a Bond object. 
    // In this project, it is called to convert a byte[] read from the UET log to UETLog object
    [Serializable]
    public class BondReader<T> where T : IBondSerializable, new()
    {
        public BondReader()
        { }
        public bool TryParse(byte[] payload, out T parsedLog)
        {
            parsedLog = default(T);

            if (payload != null)
            {
                try
                {
                    using (var ms = new MemoryStream(payload))
                    {
                        // 12 bytes of meta data
                        ms.Seek(12, SeekOrigin.Begin);

                        using (var protocolReader = new CompactBinaryProtocolReader(ms))
                        {
                            T log = new T();
                            log.Read(protocolReader);
                            parsedLog = log;

                            return true;
                        }
                    }
                }
                catch { }
            }

            return false;
        }
    }
}
