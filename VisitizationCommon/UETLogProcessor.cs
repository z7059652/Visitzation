using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.AdCenter.BI.UET.StreamingSchema;
using Microsoft.BI.Common.CryptoUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    [Serializable]
    public class UETLogProcessor
    {
        private DateTime stripeDelta;// = 
        private readonly bool filterTagIds;
        private readonly string keyFileName;

        private static UETLogProcessor instance;
        public static UETLogProcessor INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new UETLogProcessor();
                return instance;
            }
        }

        private UETLogProcessor()
        {
            stripeDelta = DateTime.Parse(@"2016-09-02 07:00:00");
            filterTagIds = true;
            keyFileName = @"20160902ip_encrypt_map.csv";
        }
        public IEnumerable<string> GetData(IEnumerable<string> data)
        {
            int count = 0;
            List<string> res = new List<string>();
            var ipDecryptor = new IPAddressDecryptor(keyFileName);
            var reader = new BondReader<UETLog>();
            var tagIdNameMap = new TagIdNameMap();
            var analyticsGuidExtractor = new AnalyticsGuidExtractor();

            foreach (var line in data)
            {
                var eqs = new EnumeratedQueryString();
                count++;
                var uetLogByte = Convert.FromBase64String(line.Split('\t')[0]);
                UETLog log;

                UETLogView vSchema = new UETLogView();
                if (!reader.TryParse(uetLogByte, out log))
                {
                    res.Add(string.Empty);
                    continue;
                }

                if (!eqs.TryParse(log.QueryString))
                {
                    res.Add(string.Empty);
                    continue;
                }

                vSchema.ReferrerURL = eqs.ReferrerURL;
                if (String.IsNullOrWhiteSpace(vSchema.ReferrerURL))
                {
                    vSchema.ReferrerURL = log.ReferrerURL;
                }

                vSchema.TagId = eqs.TagId;
                vSchema.TagName = eqs.TagName;
                if (String.IsNullOrWhiteSpace(eqs.AppInstallClickId))
                {
                    if (String.IsNullOrWhiteSpace(vSchema.ReferrerURL)
                        || log.ClientIP == null || (log.ClientIP.EncryptedIP == null && log.ClientIP.EncryptedIPv6 == null)
                        || (vSchema.TagId <= 0 && String.IsNullOrWhiteSpace(vSchema.TagName)))
                    {
                        res.Add(string.Empty);
                        continue;
                    }
                    if (vSchema.TagId <= 0)
                    {
                        if (!eqs.AdvertiserId.HasValue)
                        {
                            res.Add(string.Empty);
                            continue;
                        }
                        Dictionary<int, int> customerIdToTagId;
                        if (!tagIdNameMap.NameToIdMap.TryGetValue(vSchema.TagName, out customerIdToTagId))
                        {
                            res.Add(string.Empty);
                            continue;
                        }

                        if (!customerIdToTagId.TryGetValue(eqs.AdvertiserId.Value, out vSchema.TagId))
                        {
                            res.Add(string.Empty);
                            continue;
                        }
                    }
                    if (!CommonUtils.IsNewUETTagId(vSchema.TagId))
                    {
                        res.Add(string.Empty);
                        continue;
                    }
                    if (String.IsNullOrWhiteSpace(vSchema.TagName))
                    {
                        if (!tagIdNameMap.IdToNameMap.TryGetValue(vSchema.TagId, out vSchema.TagName))
                        {
                            vSchema.TagName = string.Empty;
                        }
                    }
                }
                vSchema.ANID = CommonUtils.ParseGuid(log.ANID);
                vSchema.ClientIP = log.ClientIP;
                vSchema.EventDateTime = CommonUtils.FromUtcUnixTimeToTicks(log.EventDateTime);
                vSchema.IsNewMUID = log.IsNewMUID;
                vSchema.LogServerName = log.LogServerName;
                vSchema.MUID = CommonUtils.ParseGuid(log.MUID);
                vSchema.QueryString = log.QueryString;
                vSchema.UserAgent = log.UserAgent;
                vSchema.AppInstallClickId = eqs.AppInstallClickId;
                vSchema.PageLoad = eqs.PageLoad;
                vSchema.PageTitle = eqs.PageTitle;
                vSchema.UETMatchingGuid = eqs.UETMatchingGuid;
                vSchema.Version = eqs.Version;

                vSchema.NavigatedFromURL = eqs.NavigatedFromURL;
                if (String.IsNullOrWhiteSpace(vSchema.NavigatedFromURL) && eqs.iframe)
                {
                    vSchema.NavigatedFromURL = log.ReferrerURL;
                }

                CustomEvent customEvent = null;
                if (String.Equals(eqs.EventType, "custom", StringComparison.OrdinalIgnoreCase))
                {
                    customEvent = new CustomEvent
                    {
                        EventCategory = eqs.EventCategory,
                        EventLabel = eqs.EventLabel,
                        EventAction = eqs.EventAction,
                        EventValue = eqs.EventValue
                    };
                }
                vSchema.customEvent = customEvent;

                vSchema.EventType = eqs.EventType == null ? null : eqs.EventType.ToLower();
                vSchema.GoalValue = eqs.GoalValue;

                Guid? analyticsGuid = null;
                if (!String.IsNullOrWhiteSpace(log.AnalyticsCookie))
                {
                    analyticsGuidExtractor.TryExtractAnalyticsGuid(log.AnalyticsCookie, out analyticsGuid);
                }
                vSchema.AnalyticsGuid = analyticsGuid;

                string ip = null;
                if (log.ClientIP != null && ipDecryptor != null)
                {
                    ip = DecryptIp(log.ClientIP, ipDecryptor);
                }
                vSchema.IP = string.IsNullOrWhiteSpace(ip) ? "hidden" : ip;

                if (String.IsNullOrWhiteSpace(ip) && log.ClientIP != null)
                {
                    ip = String.IsNullOrWhiteSpace(log.ClientIP.EncryptedIPv6) ? log.ClientIP.EncryptedIP : log.ClientIP.EncryptedIPv6;
                }

                vSchema.UAIPId = !String.IsNullOrWhiteSpace(ip) ? CommonUtils.GetGuidFromIPUserAgent(ip, log.UserAgent) : Guid.Empty;

                // Set dedup key for UET Log
                // If there is mid and rn, and IsNewMUID is false, we still dedup on mid, rn and MUID.
                // If there is mid and rn, and IsNewMUID is true, we will only dedup on mid and rn.
                // If there is no mid or rn, we’ll always dedup on timestamp and MUID.
                string dedupKey = string.Empty;
                if (eqs.UETMatchingGuid.HasValue && !String.IsNullOrWhiteSpace(eqs.rn))
                {
                    dedupKey = eqs.UETMatchingGuid.Value.ToString("N") + "-" + eqs.rn;
                    if (log.IsNewMUID == false && !String.IsNullOrEmpty(log.MUID))
                    {
                        dedupKey += "-" + log.MUID;
                    }
                }
                else
                {
                    dedupKey = log.EventDateTime.ToString();
                    if (!String.IsNullOrEmpty(log.MUID))
                    {
                        dedupKey += "-" + log.MUID;
                    }
                }
                vSchema.DedupKey = dedupKey;

                res.Add(UETLogView.Serialize(vSchema));
            }
            return res;
        }

        public string DecryptIp(ClientIPData clientIp, IPAddressDecryptor ipDecryptor)
        {
            if (!String.IsNullOrWhiteSpace(clientIp.EncryptedIP))
            {
                var ip = ipDecryptor.DecryptIP(
                    clientIp.KeyParty,
                    clientIp.KeyAlgorithm,
                    clientIp.KeyVersion,
                    clientIp.IV,
                    clientIp.EncryptedIP,
                    format: IPFormat.IPv4);

                if (!String.IsNullOrWhiteSpace(ip))
                {
                    return ip;
                }
            }

            if (!String.IsNullOrWhiteSpace(clientIp.EncryptedIPv6))
            {
                return ipDecryptor.DecryptIP(
                    clientIp.KeyParty,
                    clientIp.KeyAlgorithm,
                    clientIp.KeyVersion,
                    clientIp.IV,
                    clientIp.EncryptedIPv6,
                    format: IPFormat.IPv6);
            }

            return null;
        }
    }
}
