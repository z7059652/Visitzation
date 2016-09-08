using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    internal class CryptoWinApi
    {
        #region Const
        private const uint ALG_CLASS_HASH = (4 << 13);
        private const uint ALG_TYPE_ANY = (0);
        private const uint ALG_CLASS_DATA_ENCRYPT = (3 << 13);
        private const uint ALG_TYPE_STREAM = (4 << 9);
        private const uint ALG_TYPE_BLOCK = (3 << 9);

        private const uint ALG_SID_DES = 1;
        private const uint ALG_SID_RC4 = 1;
        private const uint ALG_SID_RC2 = 2;
        private const uint ALG_SID_MD5 = 3;
        private const uint ALG_SID_HMAC = 9;
        private const uint ALG_SID_SHA_256 = 12;
        public const uint KP_SALT = 2;
        public const uint KP_IV = 1;


        public const string MS_DEF_PROV = "Microsoft Strong Cryptographic Provider";
        //public const string MS_DEF_PROV = "Microsoft Enhanced RSA and AES Cryptographic Provider";

        public const uint PROV_RSA_FULL = 1;
        public const uint PROV_RSA_AES = 24;
        public const uint CRYPT_VERIFYCONTEXT = 0xf0000000;
        public const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        public const uint CRYPT_EXPORTABLE = 0x00000001;
        public const uint CRYPT_SILENT = 0x00000040;
        public const uint HP_HASHVAL = 0x0002;  // Hash value
        public const uint HP_HMAC_INFO = 0x0005;

        public static readonly uint CALG_MD5 = (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_MD5);
        public static readonly uint CALG_DES = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | ALG_SID_DES);
        public static readonly uint CALG_RC2 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | ALG_SID_RC2);
        public static readonly uint CALG_RC4 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_STREAM | ALG_SID_RC4);
        public static readonly uint CALG_HMAC = (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_HMAC);
        public static readonly uint CALG_SHA_256 = (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_256);
        #endregion

        const string CryptDll = "advapi32.dll";
        const string KernelDll = "kernel32.dll";

        [DllImport(CryptDll)]
        public static extern bool CryptAcquireContext(
            ref IntPtr phProv, string pszContainer, string pszProvider,
            uint dwProvType, uint dwFlags);

        [DllImport(CryptDll)]
        public static extern bool CryptReleaseContext(
            IntPtr hProv, uint dwFlags);

        [DllImport(CryptDll)]
        public static extern bool CryptImportKey(IntPtr hProv, byte[] pbData, uint dwDataLen, IntPtr hPubKey, uint dwFlags, ref IntPtr phKey);

        [DllImport(CryptDll)]
        public static extern bool CryptCreateHash(IntPtr hProv, Int32 Algid, IntPtr hKey, Int32 dwFlags, ref IntPtr phHash);

        [DllImport(CryptDll)]
        public static extern bool CryptSetKeyParam(IntPtr hProv, uint dwParam, byte[] pbData, uint dwFlags);

        [DllImport(CryptDll)]
        public static extern bool CryptSetHashParam(IntPtr hHash, Int32 dwParam, Byte[] pbData, Int32 dwFlags);


        [DllImport(CryptDll)]
        public static extern bool CryptDecrypt(
            IntPtr hKey, IntPtr hHash, bool Final, uint dwFlags,
            byte[] pbData, ref uint pdwDataLen);

        [DllImport(CryptDll)]
        public static extern bool CryptDuplicateKey(IntPtr hKey, IntPtr pdwReserved, uint dwFlags, ref IntPtr phKey);

        [DllImport(CryptDll)]
        public static extern bool CryptDuplicateHash(IntPtr hKey, IntPtr pdwReserved, uint dwFlags, ref IntPtr phKey);

        [DllImport(CryptDll)]
        public static extern bool CryptHashData(IntPtr hHash, byte[] pbData, uint dataLen, Int32 dwFlags);

        [DllImport(CryptDll)]
        public static extern bool CryptGetHashParam(IntPtr hHash, Int32 dwParam, ref byte[] pbData, ref uint pdwDataLen, Int32 dwFlags);


        public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        [DllImport(KernelDll)]
        public static extern uint GetLastError();

        [DllImport(KernelDll)]
        public static extern uint FormatMessage(
            uint dwFlags, string lpSource, uint dwMessageId,
            uint dwLanguageId, StringBuilder lpBuffer, uint nSize,
            string[] Arguments);

        public static void Failed(string command)
        {
            uint lastError = CryptoWinApi.GetLastError();
            StringBuilder sb = new StringBuilder(500);

            try
            {
                // get message for last error
                FormatMessage(CryptoWinApi.FORMAT_MESSAGE_FROM_SYSTEM,
                    null, lastError, 0, sb, 500, null);
            }
            catch
            {
                // error calling FormatMessage
                sb.Append("N/A.");
            }

        }
    }
}