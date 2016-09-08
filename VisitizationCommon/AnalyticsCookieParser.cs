using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    /// <summary>
    /// Analytics Cookie Parser
    /// One valid Analytics Cookie string looks like this
    /// --------------------------------------------------------------------------------------------------------------------------------------------
    /// CookieVersion(2 bytes) | PayLoadData | KeyVersion(8 bytes) | IV String (length decided by IVLength String) | IVLength String (2 bytes)
    /// --------------------------------------------------------------------------------------------------------------------------------------------
    /// 4v048b5a3e0adcb9fff025a754ac15c74a7c82d03526fccc41d99ea7678eaf246b0874fadea7b647b977cca158d7d7cfe6e1fc528931f88345ac754d3e49fbd9023554094c48746a89a0e3bd920cafb7aa3007f78610
    /// 4v | 048b5a3e0adcb9fff025a754ac15c74a7c82d03526fccc41d99ea7678eaf246b0874fadea7b647b977cca158d7d7cfe6e1fc528931f88345ac754d3e49fbd902 | 3554094c | 48746a89a0e3bd920cafb7aa3007f786 | 10
    /// </summary>
    [Serializable]
    internal class AnalyticsCookieParser
    {
        private const int CookieVersionLength = 2;
        private const int KeyVersionLength = 8;
        private const int IvLengthStringLength = 2;

        public byte[] IV;
        public int KeyVersion;
        public byte[] AnalyticsData;

        public bool TryParseCookieString(string cookieString)
        {
            if (String.IsNullOrWhiteSpace(cookieString))
            {
                return false;
            }

            var remainingCookieLength = cookieString.Length - IvLengthStringLength;
            if (remainingCookieLength < 0)
            {
                return false;
            }

            // Last two chars represent the IV length in hex form.
            int ivLen;
            if (!int.TryParse(cookieString.Substring(remainingCookieLength), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ivLen) || ivLen <= 0)
            {
                return false;
            }

            var ivStringLength = ivLen * 2;

            remainingCookieLength -= ivStringLength;
            if (remainingCookieLength < 0)
            {
                return false;
            }

            if (!TryParseHexStringToByteArray(cookieString.Substring(remainingCookieLength, ivStringLength), out this.IV))
            {
                return false;
            }

            remainingCookieLength -= KeyVersionLength;
            if (remainingCookieLength < 0)
            {
                return false;
            }

            var keyVerReverse = cookieString.Substring(remainingCookieLength, KeyVersionLength);

            // Reverse the bytes
            const int hexByteLen = 2;
            var sb = new StringBuilder(KeyVersionLength);
            for (var i = KeyVersionLength - hexByteLen; i >= 0; i -= hexByteLen)
            {
                sb.Append(keyVerReverse.Substring(i, hexByteLen));
            }

            if (!int.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out this.KeyVersion))
            {
                return false;
            }

            remainingCookieLength -= CookieVersionLength;
            if (remainingCookieLength <= 0 || remainingCookieLength % 2 != 0)
            {
                return false;
            }

            if (!TryParseHexStringToByteArray(cookieString.Substring(CookieVersionLength, remainingCookieLength), out this.AnalyticsData))
            {
                return false;
            }

            return true;
        }

        private static bool TryParseHexStringToByteArray(string hexString, out byte[] byteArray)
        {
            byteArray = null;

            if (hexString.Any(c => !(('0' <= c && c <= '9') || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F'))))
            {
                return false;
            }

            // The code here is trying to chop the string into substrings, 
            // each substring of length 2 and convert the "7F" string to byte value and combine the byte value to an array and return.
            byteArray = Enumerable.Range(0, hexString.Length / 2)
                .Select(x => Convert.ToByte(hexString.Substring(x * 2, 2), 16))
                .ToArray();

            return true;
        }
    }
}
