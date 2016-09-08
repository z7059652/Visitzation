//using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.BI.Common.Serialization;
using Microsoft.Bond;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers
{
    public static class CommonUtils
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

//        public static Guid GetGuidFromIPUserAgent(string clientIp, string userAgent)
//        {
//            if (String.IsNullOrWhiteSpace(clientIp))
//            {
//                throw new ArgumentException("ClientIP can't be null", "clientIp");
//            }

//            if (!String.IsNullOrWhiteSpace(userAgent))
//            {
//                userAgent = userAgent.Trim().ToLower();
//            }
//            else
//            {
//                userAgent = String.Empty;
//            }

//            return DeterministicGuid.Create(clientIp + userAgent);
//        }

//        public static uint TotalGoalCount(DateTime mapFileDate)
//        {
//            // Build Tag-to-Goals dictionary based on map files for all goal types
//            var tagToGoalsMap = new TagToGoalsMapUnion(mapFileDate);
//            return tagToGoalsMap.GetTotalGoalCount();
//        }

//        public static bool CustomerIdForGivenTag(int tagId, DateTime mapFileDate, out int tagToCustomerId)
//        {
//            var tagToCustomersMap = new TagToCustomersMap(mapFileDate);

//            var isValid = tagToCustomersMap.Map.TryGetValue(tagId, out tagToCustomerId);

//            if (isValid)
//            {
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }

//        public static bool IsAdExtensionForAppInstall(int AdExtensionPropertyId)
//        {
//            if (AdExtensionPropertyId == (int)AppInstallAdExtIds.AppPlatform ||
//                AdExtensionPropertyId == (int)AppInstallAdExtIds.AppStoreId)
//            {
//                return true;
//            }

//            return false;
//        }

//        /// <summary>
//        /// Serialize Bond data structure
//        /// </summary>
//        public static byte[] Serialize(UETLog msg)
//        {
//            using (var ms = new MemoryStream())
//            {
//                WriteMeta(ms, 66216318, 70605046);
//                using (var writer = new CompactBinaryProtocolWriter(ms))
//                {
//                    msg.Write(writer);
//                    return ms.ToArray();
//                }
//            }
//        }

//        public static byte[] Serialize(UserIdCoveragePair msg)
//        {
//            using (var ms = new MemoryStream())
//            {
//                WriteMeta(ms, 66216318, 70605046);
//                using (var writer = new CompactBinaryProtocolWriter(ms))
//                {
//                    msg.Write(writer);
//                    return ms.ToArray();
//                }
//            }
//        }

//        /// <summary>
//        /// Write meta data for serialized Bond data structure 
//        /// </summary>
//        /// <param name="stream"></param>
//        /// <param name="typenameHash"></param>
//        /// <param name="protocolHash"></param>
//        private static void WriteMeta(Stream stream, UInt32 typenameHash, UInt32 protocolHash)
//        {
//            byte[] typenameMeta = BitConverter.GetBytes(typenameHash);
//            byte[] protocolMeta = BitConverter.GetBytes(protocolHash);
//            byte[] sizeMeta = BitConverter.GetBytes(typenameMeta.Length + protocolMeta.Length + sizeof(int));

//            stream.Write(sizeMeta, 0, sizeMeta.Length);
//            stream.Write(typenameMeta, 0, typenameMeta.Length);
//            stream.Write(protocolMeta, 0, protocolMeta.Length);
//        }

        /// <summary>
        /// Try to parse the Guid from the Guid string
        /// </summary>
        /// <param name="guidString"> Input Guid string </param>
        /// <returns> The parsed Guid value if successfully parse, otherwise null. </returns>
        public static Guid? ParseGuid(string guidString)
        {
            Guid guid;
            if (Guid.TryParse(guidString, out guid))
            {
                return guid;
            }

            return null;
        }

//        public static string Serialize(AttributionFacts fact)
//        {
//            if (fact == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();
//            sb.AppendFormat("AttributedClickType={0},", fact.AttributedClickType);
//            if (fact.Conversions != null)
//            {
//                foreach (Conversion conversion in fact.Conversions)
//                {
//                    sb.AppendFormat(",Conversion({0})", Serialize(conversion));
//                }
//            }

//            if (fact.Events != null)
//            {
//                foreach (UETEvent uetEvent in fact.Events)
//                {
//                    sb.AppendFormat(",UETEvent({0})", Serialize(uetEvent));
//                }
//            }

//            if (fact.UserAdActivities != null)
//            {
//                foreach (UserAdActivityDetail uaa in fact.UserAdActivities)
//                {
//                    sb.AppendFormat(", UserAdActivityDetail({0})", Serialize(uaa));
//                }
//            }

//            return sb.ToString();
//        }

//        public static string Serialize(Conversion conversion)
//        {
//            if (conversion == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();

//            sb.AppendFormat("GoalId={0}, GoalValue={1}, IsClickWithinLookbackWindow={2}, GoalLookbackWindow={3}, EventIndex={4}, GoalTrackingType={5}, ",
//                            conversion.GoalId, conversion.GoalValue, conversion.IsClickWithinLookbackWindow,
//                            conversion.GoalLookbackWindow, conversion.EventsIndex32[0], conversion.GoalTrackingType);

//            sb.AppendFormat("FirstConversion={0} NumSearchClickAssists32={1}, AccountId={2}, AttributedClickId={3}, ConversionDateTime={4}, ",
//                conversion.FirstConversion, conversion.NumSearchClickAssists32, conversion.AccountId ?? -1, conversion.AttributedClickId ?? string.Empty,
//                conversion.ConversionDateTime);

//            if (conversion.SearchClickAssistIndexList != null)
//            {
//                foreach (UserAdActivityIndex assistIndex in conversion.SearchClickAssistIndexList)
//                {
//                    StringBuilder stringBuilder = sb.AppendFormat("ActivityIndex={0}, ActivityGroupIndex={1},", assistIndex.ActivityIndex, assistIndex.ActivityGroupIndex);
//                }
//            }

//            return sb.ToString();
//        }

//        public static string Serialize(UserAdActivitySummary uas)
//        {
//            if (uas == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();
//            sb.AppendFormat("Type={0}, ", uas.Type);
//            sb.Append(Serialize(uas.UserAdActivities));
//            return sb.ToString();
//        }

//        public static string Serialize(List<UserAdActivityDetail> uaalist)
//        {
//            if (uaalist == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();
//            foreach (UserAdActivityDetail uaa in uaalist)
//            {
//                sb.AppendFormat("UAA=({0})", Serialize(uaa));
//            }

//            return sb.ToString();
//        }

//        public static string Serialize(UserAdActivityDetail uaa)
//        {
//            if (uaa == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();
//            sb.AppendFormat("AdId={0}, AdUnitID={1}, AdvertiserAccountId={2}, Advertiserid={10}, CustomerId={3}, DeliveredLocationId={4}, OrderItemId={5}, UTCOffset={6}" +
//                            ", SearchClick.Count={7}, DisplayClick.Count={8}, DisplayImpression.Count={9}, "
//                            , uaa.AdId, uaa.AdUnitID, uaa.AdvertiserAccountId, uaa.CustomerId, uaa.DeliveredLocationId, uaa.OrderItemId, uaa.UTCOffset
//                            , (uaa.SearchClick == null ? 0 : uaa.SearchClick.Count)
//                            , (uaa.DisplayClick == null ? 0 : uaa.DisplayClick.Count)
//                            , (uaa.DisplayImpression == null ? 0 : uaa.DisplayImpression.Count)
//                            , uaa.AdvertiserId
//                            );
//            sb.Append(Serialize(uaa.SearchClick));
//            return sb.ToString();
//        }

//        public static string Serialize(List<SearchClick> searchClickList)
//        {
//            if (searchClickList == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();
//            foreach (var searchClick in searchClickList)
//            {
//                if (searchClick != null)
//                {
//                    sb.AppendFormat("ClickId = {0}, ClickDateTime= {1}", searchClick.ClickId, searchClick.ClickDateTime);
//                }
//            }

//            return sb.ToString();
//        }

//        public static string Serialize(UserAdActivityIndex uaaIndex)
//        {
//            if (uaaIndex == null)
//            {
//                return String.Empty;
//            }

//            var sb = new StringBuilder();
//            sb.AppendFormat("ActivityGroupIndex={0}, ActivityIndex={1}, ", uaaIndex.ActivityGroupIndex, uaaIndex.ActivityIndex);
//            return sb.ToString();
//        }

//        public static string Serialize(UETEvent evnt)
//        {
//            if (evnt == null)
//            {
//                return string.Empty;
//            }

//            var sb = new StringBuilder();
//            sb.AppendFormat("[evnt.EventDateTime={0}, evnt.GoalValue={1}, evnt.LogServerName={2}, evnt.NavigatedFromURL={3}" +
//                            " evnt.PageLoad={4}, evnt.PageTitle={5}, evnt.ReferrerURL={6}, evnt.TimeOnPrevPage={7}, evnt.Version={8}, evnt.Type={9}",
//                            new DateTime(evnt.EventDateTime), evnt.GoalValue, evnt.LogServerName, evnt.NavigatedFromURL,
//                            evnt.PageLoad, evnt.PageTitle, evnt.ReferrerURL, evnt.TimeOnPrevPage,
//                            evnt.Version, evnt.EventType);

//            if (evnt.UETMatchingGuid != null)
//            {
//                sb.AppendFormat(", evnt.UETMatchingGuid={0}", evnt.UETMatchingGuid.ToSystemGuid().ToString());
//            }

//            if (evnt.customEvent != null)
//            {
//                var customEvent = evnt.customEvent;
//                sb.AppendFormat(", customEvent.Category={0}, customEvent.Action={1}, customEvent.Label={2}, customEvent.Value={3},"
//                , customEvent.EventCategory, customEvent.EventAction, customEvent.EventLabel, customEvent.EventValue);
//            }

//            sb.Append("]");
//            return sb.ToString();
//        }

//        public static long FromUtcUnixTimeToTicks(long unixTime)
//        {
//            return Epoch.AddSeconds(unixTime).Ticks;
//        }

//        public static byte[] ExtractMZObject(Bond.BondBlob blobData)
//        {
//            byte[] byteArray = null;
//            if (blobData != null && blobData.Data != null)
//            {
//                ArraySegment<byte> arraySegment = blobData.Data;
//                byteArray = new byte[arraySegment.Count];

//                for (int index = 0; index < arraySegment.Count; index++)
//                {
//                    byteArray[index] = arraySegment.Array[arraySegment.Offset + index];
//                }

//            }

//            return byteArray;

//        }

//        public static byte[] ReserializeCfrObject<T>(Bond.BondBlob blob, CFRLogRecordReseralizeHandler<T> handler)
//        {
//            if (blob == null)
//            {
//                return null;
//            }

//            byte[] binary = CommonUtils.ExtractMZObject(blob);
//            T cfrLogRecordObject = handler(binary);

//            if (cfrLogRecordObject == null)
//            {
//                return null;
//            }

//            return ((ILogSerializable)cfrLogRecordObject).Serialize();
//        }

//        public delegate T CFRLogRecordReseralizeHandler<T>(byte[] binary);

//        public static bool HasConversions(SAEventConversionFacts SAEventConversionFactsRow)
//        {
//            if (SAEventConversionFactsRow != null && SAEventConversionFactsRow.Visits != null)
//            {
//                foreach (Visit visit in SAEventConversionFactsRow.Visits)
//                {
//                    if (visit.Conversions != null && visit.Conversions.Count > 0)
//                    {
//                        return true;
//                    }
//                }
//            }

//            return false;
//        }
    }
}
