
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    [Serializable]
    public class AESwithHMACDecryption
    {
        // dictionary to store version and keys
        private readonly Dictionary<uint, IntPtr> keyMap;

        public AESwithHMACDecryption(string keyFiles)
        {
            keyMap = new Dictionary<uint, IntPtr>();
            foreach (string keyFile in keyFiles.Split(','))
            {
                GetKey(keyFile);
            }
        }

        public bool DecryptString(uint version, byte[] pbSaltBlob, ref byte[] EncryptBuffer, ref uint dwInLen)
        {
            IntPtr enryptionKey;
            if (!keyMap.TryGetValue(version, out enryptionKey))
            {
                return false;
            }

            IntPtr hAESKey = IntPtr.Zero;
            if (!CryptoWinApi.CryptDuplicateKey(enryptionKey, IntPtr.Zero, 0, ref hAESKey))
            {
                CryptoWinApi.Failed("CryptDuplicateKey");
                return false;
            }

            // assign salt to the key
            if (!CryptoWinApi.CryptSetKeyParam(hAESKey, CryptoWinApi.KP_IV, pbSaltBlob, 0))
            {
                CryptoWinApi.Failed("CryptSetKeyParam");
                return false;
            }

            if (!CryptoWinApi.CryptDecrypt(hAESKey, IntPtr.Zero, true, 0, EncryptBuffer, ref dwInLen))
            {
                CryptoWinApi.Failed("CryptDecrypt");
                return false;
            }

            return true;
        }

        private bool GetKey(string keyFile)
        {
            //try
            //{
                using (var fs = new FileStream(keyFile, FileMode.Open, FileAccess.Read))
                {
                    using (var br = new BinaryReader(fs))
                    {
                        br.ReadUInt32(); // recordSize
                        var keyVersion = br.ReadUInt32();
                        br.ReadUInt32(); // IVBlobLength
                        br.ReadUInt32(); // ElapsedTime
                        var keyBlobLength = br.ReadUInt32();
                        br.ReadUInt32(); // hashKeyBlobLength
                        var encryptionKeyBlob = br.ReadBytes((int)keyBlobLength);

                        IntPtr hCryptProv = IntPtr.Zero;
                        if (!CryptoWinApi.CryptAcquireContext(ref hCryptProv, null, null, CryptoWinApi.PROV_RSA_AES, CryptoWinApi.CRYPT_VERIFYCONTEXT | CryptoWinApi.CRYPT_MACHINE_KEYSET | CryptoWinApi.CRYPT_SILENT))
                        {

                            return false;
                        }

                        var enryptionKey = IntPtr.Zero;
                        if (!CryptoWinApi.CryptImportKey(hCryptProv, encryptionKeyBlob, keyBlobLength, IntPtr.Zero, 0, ref enryptionKey))
                        {

                            return false;
                        }

                        keyMap.Add(keyVersion, enryptionKey);

                        return true;
                    }
                }
            //}
            //catch (Exception)
            //{

            //    return false;
            //}
        }
    }
}
