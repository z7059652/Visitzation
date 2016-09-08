using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    public class CommonUtils
    {
        private const int NewUETTagIdSeedValue = 4000000;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public enum AppInstallAdExtIds
        {
            AppPlatform = 55,
            AppStoreId = 56
        }

        public static bool IsNewUETTagId(int tagId)
        {
            return tagId >= NewUETTagIdSeedValue;
        }

        public static Guid? ParseGuid(string guidString)
        {
            Guid guid;
            if (Guid.TryParse(guidString, out guid))
            {
                return guid;
            }

            return null;
        }

        public static long FromUtcUnixTimeToTicks(long unixTime)
        {
            return Epoch.AddSeconds(unixTime).Ticks;
        }

        public static Guid GetGuidFromIPUserAgent(string clientIp, string userAgent)
        {
            if (String.IsNullOrWhiteSpace(clientIp))
            {
                throw new ArgumentException("ClientIP can't be null", "clientIp");
            }

            if (!String.IsNullOrWhiteSpace(userAgent))
            {
                userAgent = userAgent.Trim().ToLower();
            }
            else
            {
                userAgent = String.Empty;
            }

            return DeterministicGuid.Create(clientIp + userAgent);
        }
    }
}
