
using System;
using System.Text;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    /// <summary>
    /// Extractor used to parse the AnalyticsGuid from the encryped AnalyticsCookie
    /// </summary>
    [Serializable]
    public class AnalyticsGuidExtractor
    {
        private readonly AESwithHMACDecryption decryptProvider;
        private readonly AnalyticsCookieParser cookieParser;

        public AnalyticsGuidExtractor()
        {
            decryptProvider = new AESwithHMACDecryption("encryptionkeyAES.dat");
            cookieParser = new AnalyticsCookieParser();
        }

        public bool TryExtractAnalyticsGuid(string encryptedAnalyticsCookie, out Guid? analyticsGuid)
        {
            analyticsGuid = null;

            if (cookieParser.TryParseCookieString(encryptedAnalyticsCookie))
            {
                var bufferLen = (uint)cookieParser.AnalyticsData.Length;
                if (!decryptProvider.DecryptString((uint)cookieParser.KeyVersion, cookieParser.IV, ref cookieParser.AnalyticsData, ref bufferLen) ||
                    cookieParser.AnalyticsData == null ||
                    bufferLen <= 0)
                {
                    // TODO: we have about 1600 rows with KeyVersion 1312998531, while the majority of rows (7 mil) have KeyVersion 1275679797
                    return false;
                }

                var decryptedString = Encoding.ASCII.GetString(cookieParser.AnalyticsData, 0, (int)bufferLen);
                // decryptedString should be like: Ver=1.0|ts=1450665191|ag=9dc9229b101b40378c3f3d10f41f1aee
                var agStringTokens = decryptedString.Split('|');
                const string agPrefix = "ag=";
                if (agStringTokens.Length != 3 || !agStringTokens[2].StartsWith(agPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                analyticsGuid = CommonUtils.ParseGuid(agStringTokens[2].Substring(agPrefix.Length));
                return analyticsGuid.HasValue;
            }

            return false;
        }
    }
}
