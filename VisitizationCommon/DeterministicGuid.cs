using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    public static class DeterministicGuid
    {
        public static readonly Guid DnsNamespace = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        public static readonly Guid UrlNamespace = new Guid("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
        public static readonly Guid IsoOidNamespace = new Guid("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

        public static Guid Create(string name)
        {
            return Create(DnsNamespace, name, 5);
        }

        public static Guid Create(Guid namespaceId, string name)
        {
            return Create(namespaceId, name, 5);
        }

        public static Guid Create(Guid namespaceId, string name, int version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name: cannot be null or empty");
            if (version != 3 && version != 5)
                throw new ArgumentOutOfRangeException("version", "version must be either 3 or 5.");

            byte[] nameBytes = Encoding.UTF8.GetBytes(name);


            byte[] namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes);

            // comput the hash of the name space ID 
            byte[] hash;
            using (HashAlgorithm algorithm = version == 3 ? (HashAlgorithm)MD5.Create() : SHA1.Create())
            {
                algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
                algorithm.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
                hash = algorithm.Hash;
            }


            byte[] newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            // set the four most significant bits (bits 12 through 15) of the time_hi_and_version field to the appropriate 4-bit version number from Section 4.1.3 (step 8)
            newGuid[6] = (byte)((newGuid[6] & 0x0F) | (version << 4));

            // set the two most significant bits (bits 6 and 7) of the clock_seq_hi_and_reserved to zero and one, respectively (step 10)
            newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

            // convert the resulting UUID to local byte order (step 13)
            SwapByteOrder(newGuid);
            return new Guid(newGuid);
        }

        // Converts a GUID (expressed as a byte array) to/from network order (MSB-first).
        internal static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        private static void SwapBytes(byte[] guid, int left, int right)
        {
            byte temp = guid[left];
            guid[left] = guid[right];
            guid[right] = temp;
        }
    }
}
