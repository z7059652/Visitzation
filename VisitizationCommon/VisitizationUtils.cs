using System;
using System.Globalization;
using System.Text;
using Microsoft.AdCenter.BI.UET.Schema;


namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    public enum DefectiveReason : int
    {
        // MISSING_VERSION = 1,
        MISSING_REFERRER_URL = 2,
        MISSING_TAGID_AND_TAGNAME = 3,
        MISSING_IP = 4,
        FAILED_TO_PARSE = 5,
        DUPLICATEKEY_IN_QS = 6,
        FAILED_TO_DECRYPT_IP = 7,
        FAILED_MAP_TAGNAME_TO_TAGID = 8,
        TAGID_NOTFOUND_FORCUSTOMER = 9
    }
    [Serializable]
    public class VisitizationUtils
    {
        public const string EventTypeCustom = "custom";
        public const string EventTypePageLoad = "pageload";

        /// <summary>
        /// Used only in test project LogDataGenerator.
        /// Build the query string from the EnumeratedQueryString struct.
        /// </summary>
        public static string BuildQueryString(EnumeratedQueryString enumerated, char keyValueSeparator = '=', char groupSeparator = '&')
        {
            var qsParameters = new string[13];

            qsParameters[0] = enumerated.Version != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterVersion, keyValueSeparator, enumerated.Version) : null;  // Version
            qsParameters[1] = enumerated.PageTitle != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterPageTitle, keyValueSeparator, enumerated.PageTitle) : null; // PageTitle
            qsParameters[2] = enumerated.NavigatedFromURL != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterNavigatedFromUrl, keyValueSeparator, enumerated.NavigatedFromURL) : null; // NavigatedFromURL
            qsParameters[3] = enumerated.EventCategory != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterEventCategory, keyValueSeparator, enumerated.EventCategory) : null; // EventCategory
            qsParameters[4] = enumerated.EventAction != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterEventAction, keyValueSeparator, enumerated.EventAction) : null; // EventAction
            qsParameters[5] = enumerated.EventLabel != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterEventLabel, keyValueSeparator, enumerated.EventLabel) : null; // EventLabel
            qsParameters[6] = enumerated.EventValue != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterEventValue, keyValueSeparator, enumerated.EventValue) : null; // EventValue
            qsParameters[7] = enumerated.EventType != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterEventType, keyValueSeparator, enumerated.EventType) : null; // EventType
            qsParameters[8] = enumerated.TagId != -1 ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterTagId, keyValueSeparator, enumerated.TagId) : null; // TagId
            qsParameters[9] = enumerated.PageLoad != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterPageLoad, keyValueSeparator, enumerated.PageLoad) : null; // PageLoad
            qsParameters[10] = enumerated.GoalValue != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterGoalValue, keyValueSeparator, enumerated.GoalValue) : null; // GoalValue
            qsParameters[11] = enumerated.TagName != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QsParameterTagName, keyValueSeparator, enumerated.TagName) : null; // TagName
            qsParameters[12] = enumerated.UETMatchingGuid != null ? String.Format("{0}{1}{2}", EnumeratedQueryString.QSParameterUETMatchingMUID, keyValueSeparator, enumerated.UETMatchingGuid) : null; // UETMatchingGuid

            var queryString = String.Join(groupSeparator.ToString(CultureInfo.InvariantCulture), qsParameters);

            return queryString;
        }

        /// <summary>
        /// Convert the time, in minutes, provided as string to ticks.
        /// It is used for calculating the session timeout threshold and job execution interval
        /// </summary>
        public static long GetTickesFromMinutes(string timeInMinutes)
        {
            return Int32.Parse(timeInMinutes) * 60L * 10000000;
        }

        /// <summary>
        /// Convert the time, in hours, provided as string to ticks.
        /// </summary>
        public static long GetTicksFromHours(string timeInHours)
        {
            return Int32.Parse(timeInHours) * 60L * 60 * 10000000;
        }

        /// <summary>
        /// Decides if two events should be grouped as part of the same visit or not
        /// This logic is used in both visitization and escrow
        /// </summary>
        public static bool AreEventGroupsInTheSameVisit(
            UETEvent currentEvent,
            long prevEventDateTime,
            string prevReferrerUrl,
            long sessionTimeoutThreshold)
        {
            var currentNavigatedFromUrl = String.Equals(currentEvent.EventType, VisitizationUtils.EventTypePageLoad, StringComparison.OrdinalIgnoreCase) ? currentEvent.NavigatedFromURL : currentEvent.ReferrerURL;

            // For each two consecutive events, assign the same VisitId to them if:
            // SecondEvent.NavigatedFromURL == FirstEvent.ReferrerURL
            // AND
            // Their time difference is less than SessionTimeoutThresholdInMunites
            var timeDiff = (currentEvent.EventDateTime - prevEventDateTime);
            return (timeDiff >= 0 &&
                    timeDiff <= sessionTimeoutThreshold &&
                    GetHostURL(currentNavigatedFromUrl) == GetHostURL(prevReferrerUrl));
        }

        /// <summary>
        /// Extracts the host url from the full url. For example, the host for http://r.msn.com/path is r.msn.com
        /// </summary>
        public static string GetHostURL(string url)
        {
            // If the url string is not a uri, then return the original url as is 
            string host = url;
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                host = uri.Host.ToLower();
            }

            return host;
        }

        /// <summary>
        /// Create the hashed VisitId (long type) from the ANID, MUID, EventDateTime and ReferrerURL. 
        /// Concat all the strings use DeterministicGuid.Create to create the Guid and convert it to long 
        /// </summary>
        /// <returns>A uniquely generated long from these 4 fields.</returns>
        public static ulong GenerateVisitId(Guid? ANID, Guid? MUID, long EventDateTime, string ReferrerURL)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ANID == null ? string.Empty : ANID.ToString());
            sb.Append(MUID == null ? string.Empty : MUID.ToString());
            sb.Append(EventDateTime);
            sb.Append(ReferrerURL);
            Guid guid = DeterministicGuid.Create(sb.ToString());
            byte[] guidBytes = guid.ToByteArray();
            ulong visitId = BitConverter.ToUInt64(guidBytes, 0);
            return visitId;
        }

        public static string Serialize(EscrowVisitRecord escrowRow)
        {
            if (escrowRow == null)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            sb.AppendFormat("VisitId={0}, LastEventDateTime={1}, LastEventIsNewMUID={2}, " +
                            "LastEventReferrerURL={3}, IsJoined={4}, VisitStatistics={5}",
                            escrowRow.VisitId, escrowRow.LastEventDateTime, escrowRow.LastEventIsNewMUID,
                            escrowRow.LastEventReferrerURL, escrowRow.IsJoined, Serialize(escrowRow.Statistic));
            return sb.ToString();
        }

        public static string Serialize(Conversion conversion)
        {
            if (conversion == null)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();

            sb.AppendFormat("GoalId={0}, GoalValue={1}, IsClickWithinLookbackWindow={2}, GoalLookbackWindow={3}, EventIndex={4} ",
                            conversion.GoalId, conversion.GoalValue, conversion.IsClickWithinLookbackWindow,
                            conversion.GoalLookbackWindow, conversion.EventsIndex32[0]);
            return sb.ToString();
        }

        public static string Serialize(VisitStatistic stat)
        {
            if (stat == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("NumberOfPages={0}, VisitStartDateTime={1}", stat.NumberOfPages, new DateTime(stat.VisitStartDateTime));
            var sb_goal = new StringBuilder();
            if (stat.AchievedGoals != null)
            {
                foreach (var goalId in stat.AchievedGoals)
                {
                    sb_goal.AppendFormat("{0};", goalId);
                }
            }

            sb.AppendFormat(" ,AchievedGoals={0}", sb_goal.ToString());
            return sb.ToString();
        }

        public static string Serialize(SAEventConversionFacts fact)
        {
            if (fact == null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.AppendFormat("ANID={0}, MUID={1}, IsNewMUID={2}, ",
                            (fact.ANID == null ? string.Empty : fact.ANID.ToSystemGuid().ToString()),
                            (fact.MUID == null ? string.Empty : fact.MUID.ToSystemGuid().ToString()),
                            fact.IsNewMUID);

            foreach (Visit visit in fact.Visits)
            {
                sb.AppendFormat(",Visit({0})", Serialize(visit));
            }

            return sb.ToString();
        }

        public static string Serialize(Visit visit)
        {
            if (visit == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            sb.AppendFormat("[VisitId={0}, IsContinuous={1}, Bounce={2}, Statistic={3}]",
                visit.VisitId, visit.IsContinuous, visit.Bounce, Serialize(visit.Statistic));

            foreach (UETEvent evnt in visit.Events)
            {
                sb.AppendFormat(",Event({0})", Serialize(evnt));
            }

            if (visit.Conversions != null)
            {
                foreach (Conversion conversion in visit.Conversions)
                {
                    sb.AppendFormat(",Conversion({0})", Serialize(conversion));
                }
            }

            return sb.ToString();
        }

        public static string Serialize(UETEvent evnt)
        {
            if (evnt == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("[evnt.EventDateTime={0}, evnt.GoalValue={1}, evnt.LogServerName={2}, evnt.NavigatedFromURL={3}" +
                            " evnt.PageLoad={4}, evnt.PageTitle={5}, evnt.ReferrerURL={6}, evnt.TimeOnPrevPage={7}, evnt.Version={8}, evnt.Type={9}",
                            new DateTime(evnt.EventDateTime), evnt.GoalValue, evnt.LogServerName, evnt.NavigatedFromURL,
                            evnt.PageLoad, evnt.PageTitle, evnt.ReferrerURL, evnt.TimeOnPrevPage,
                            evnt.Version, evnt.EventType);

            if (evnt.UETMatchingGuid != null)
            {
                sb.AppendFormat(", evnt.UETMatchingGuid={0}", evnt.UETMatchingGuid.ToSystemGuid().ToString());
            }

            if (evnt.customEvent != null)
            {
                var customEvent = evnt.customEvent;
                sb.AppendFormat(", customEvent.Category={0}, customEvent.Action={1}, customEvent.Label={2}, customEvent.Value={3},"
                , customEvent.EventCategory, customEvent.EventAction, customEvent.EventLabel, customEvent.EventValue);
            }

            sb.Append("]");
            return sb.ToString();
        }

        public static SAEventConversionFacts CombineConversionFactsAndEscrow(SAEventConversionFacts facts, EscrowVisitRecord escrow, string sessionTimeoutThresholdString)
        {
            var sessionTimeoutThreshold = GetTickesFromMinutes(sessionTimeoutThresholdString);

            var currentVisit = facts.Visits[0];
            var currentFirstEvent = currentVisit.Events[0];

            var groupEvents = AreEventGroupsInTheSameVisit(
                currentFirstEvent,
                escrow.LastEventDateTime,
                escrow.LastEventReferrerURL,
                sessionTimeoutThreshold);

            if (groupEvents)
            {
                // Copy VisitId
                currentVisit.VisitId = escrow.VisitId == 0 ? (ulong)escrow.VisitIdint : escrow.VisitId;

                // Copy Statistics
                currentVisit.Statistic = escrow.Statistic;
            }

            return facts;
        }
    }
}
