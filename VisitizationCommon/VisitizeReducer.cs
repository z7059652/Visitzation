using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.AdCenter.BI.UET.StreamingSchema;
using Microsoft.BI.Common;
using Microsoft.Bond;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    [Serializable]
    public class VisitizeReducer
    {
        private const char delimeter = '\n';
        private const string GroupKeyNewMuids = "NewMUIDs";
        private long sessionTimeoutThreshold = 30;
        private const int maxEventsPerVisit = 10000;
        private const int maxVisitsPerIPTag = 10000;
        public long endOfDelta;
        private static VisitizeReducer instance = null;
        public static VisitizeReducer INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new VisitizeReducer();
                return instance;
            }
        }


        private VisitizeReducer()
        {
            DateTime dt = DateTime.Parse(@"2016-09-02 06:00:00").AddMinutes(60);
            endOfDelta = dt.Ticks;
            sessionTimeoutThreshold = VisitizationUtils.GetTickesFromMinutes(sessionTimeoutThreshold.ToString());
        }


        public IEnumerable<string> GetData(KeyValuePair<string, string> line)
        {
            var uetLogsWithSameUAIPTag = line.Value.Split(new char[] { delimeter }, StringSplitOptions.RemoveEmptyEntries);
            //List<string> output = new List<string>();
            var eventGroups = new Dictionary<string, List<UETEvent>>();
            var firstRow = true;
            ClientIPData clientIP = null;
            var visitization = new VisitizationSchema();
            foreach(var uetLogStr in uetLogsWithSameUAIPTag)
            {
                var uetLog = UETLogView.Deserialize(uetLogStr);
                if (uetLog == null)
                    continue;
                if(firstRow)
                {
                    visitization.UAIPId = uetLog.UAIPId;
                    visitization.TagId = uetLog.TagId;
                    visitization.TagName = uetLog.TagName;
                    //visitization.UserAgent = uetLog.UserAgent;  //why UserAgent is needed in the Original VisitizationReducer

                    clientIP = uetLog.ClientIP;
                    firstRow = false;
                }
                var anid = uetLog.ANID;
                var muid = uetLog.MUID;
                var isNewMuid = uetLog.IsNewMUID;
                var analyticsGuid = uetLog.AnalyticsGuid;
                var uetMatchingGuid = uetLog.UETMatchingGuid;

                var currentUetEvent = new UETEvent();
                currentUetEvent.EventDateTime = uetLog.EventDateTime;
                currentUetEvent.ANID = (!anid.HasValue) ? null : new GUID(anid.Value);
                currentUetEvent.MUID = (!muid.HasValue) ? null : new GUID(muid.Value);
                currentUetEvent.IsNewMUID = isNewMuid;
                currentUetEvent.ReferrerURL = uetLog.ReferrerURL;
                currentUetEvent.PageTitle = uetLog.PageTitle;
                currentUetEvent.customEvent = uetLog.customEvent;
                currentUetEvent.NavigatedFromURL = uetLog.NavigatedFromURL;
                currentUetEvent.GoalValue = uetLog.GoalValue;
                currentUetEvent.PageLoad = uetLog.PageLoad;
                currentUetEvent.Version = uetLog.Version;
                currentUetEvent.LogServerName = uetLog.LogServerName;
                currentUetEvent.EventType = uetLog.EventType;
                currentUetEvent.AnalyticsGuid = (!analyticsGuid.HasValue) ? null : new GUID(analyticsGuid.Value);
                currentUetEvent.UETMatchingGuid = (!uetMatchingGuid.HasValue) ? null : new GUID(uetMatchingGuid.Value);

                AddToEventGroups(eventGroups, anid, muid, isNewMuid, currentUetEvent);
            }
            List<UETEvent> newMuidsList;
            if(eventGroups.TryGetValue(GroupKeyNewMuids, out newMuidsList))
            {
                var eventsToBeRemoved = new List<UETEvent>();

                foreach (var uetEvent in newMuidsList)
                {
                    List<UETEvent> events;
                    if (eventGroups.TryGetValue(uetEvent.MUID.ToSystemGuid().ToString(), out events))
                    {
                        eventsToBeRemoved.Add(uetEvent);
                        events.Insert(0, uetEvent);
                    }
                }

                foreach (var ev in eventsToBeRemoved)
                {
                    newMuidsList.Remove(ev);
                }
            }
            foreach (var eventGroup in eventGroups)
            {
                // Build the Visit List from the event groups
                var events = eventGroup.Value;
                if (events.Count > 0)
                {
                    var factObject = BuildVisitEventGroupFromEventsList(sessionTimeoutThreshold, endOfDelta, events, maxEventsPerVisit, maxVisitsPerIPTag);
                    if (factObject.Visits.Count > 0)
                    {
                        factObject.ClientIP = clientIP;

                        var firstEvent = events[0];
                        factObject.ANID = firstEvent.ANID;
                        factObject.MUID = firstEvent.MUID;
                        factObject.IsNewMUID = firstEvent.IsNewMUID;

                        visitization.AnalyticsGuid = firstEvent.AnalyticsGuid == null ? (Guid?)null : firstEvent.AnalyticsGuid.ToSystemGuid();

                        FixCustomGoalValues(factObject);
                        visitization.SAEventConversionFactsRow = factObject;
                        yield return VisitizationSchema.Serialize(visitization);
                        //output.Add(VisitizationSchema.Serialize(visitization));
                    }
                }
            }
            //return output;
        }

        private static Visit InitializeNewVisit(ulong visitId, long eventDateTime, long sessionTimeoutThreshold, long endOfDelta)
        {
            // Create a new Visit
            var newVisit = new Visit();
            newVisit.VisitId = visitId;
            newVisit.Statistic = new VisitStatistic { VisitStartDateTime = eventDateTime };
            newVisit.Events = new List<UETEvent>();

            return newVisit;
        }

        private static SAEventConversionFacts BuildVisitEventGroupFromEventsList(long sessionTimeoutThreshold, long endOfDelta, List<UETEvent> uetEvents, int maxEventsPerVisit, int maxVisitsPerIPTag)
        {
            var factObject = new SAEventConversionFacts { Visits = new List<Visit>() };

            long prevDateTime = 0;
            string prevReferrer = string.Empty;

            foreach (var uetEvent in uetEvents)
            {
                if (!VisitizationUtils.AreEventGroupsInTheSameVisit(uetEvent, prevDateTime, prevReferrer, sessionTimeoutThreshold))
                {
                    // Not the same group, Create a new Visit and add this event to be the first one.

                    if (factObject.Visits.Count == maxVisitsPerIPTag)
                    {
                        // Visits within the IPTag already reach the limit, we should break and stop adding new Visits.
                        break;
                    }

                    // Create a new Visit and add it to the Visits list
                    var visitId = VisitizationUtils.GenerateVisitId(
                        uetEvent.ANID == null ? null : (Guid?)uetEvent.ANID.ToSystemGuid(),
                        uetEvent.MUID == null ? null : (Guid?)uetEvent.MUID.ToSystemGuid(),
                        uetEvent.EventDateTime,
                        uetEvent.ReferrerURL);

                    var newVisit = InitializeNewVisit(visitId, uetEvent.EventDateTime, sessionTimeoutThreshold, endOfDelta);
                    newVisit.Events.Add(uetEvent);
                    factObject.Visits.Add(newVisit);
                }
                else
                {
                    // Add the event to the last Visit in the Visits list
                    var lastVisit = factObject.Visits[factObject.Visits.Count - 1];
                    if (lastVisit.Events.Count < maxEventsPerVisit)
                    {
                        lastVisit.Events.Add(uetEvent);
                    }
                }

                prevDateTime = uetEvent.EventDateTime;
                prevReferrer = uetEvent.ReferrerURL;
            }

            return factObject;
        }

        private static bool IsCustomGoalEvent(UETEvent evnt)
        {
            // TODO: Curious about the logic. Those fields are emtpy, yet still makes it a customEvent.
            if (evnt != null &&
                evnt.customEvent != null &&
                String.IsNullOrEmpty(evnt.customEvent.EventAction) &&
                String.IsNullOrEmpty(evnt.customEvent.EventCategory) &&
                String.IsNullOrEmpty(evnt.customEvent.EventLabel) &&
                evnt.customEvent.EventValue == null &&
                evnt.GoalValue.HasValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void FixCustomGoalValues(SAEventConversionFacts factObject)
        {
            var midGVTable = new Dictionary<Guid, double>();
            foreach (var visit in factObject.Visits)
            {
                foreach (var evnt in visit.Events)
                {
                    if (IsCustomGoalEvent(evnt) && evnt.UETMatchingGuid != null && evnt.GoalValue.HasValue)
                    {
                        double currentGoal;
                        var mid = evnt.UETMatchingGuid.ToSystemGuid();
                        if (midGVTable.TryGetValue(mid, out currentGoal))
                        {
                            if (currentGoal < evnt.GoalValue)
                            {
                                midGVTable[mid] = evnt.GoalValue.Value;
                            }
                        }
                        else
                        {
                            midGVTable.Add(mid, evnt.GoalValue.Value);
                        }
                    }
                }
            }

            foreach (var visit in factObject.Visits)
            {
                foreach (var evnt in visit.Events)
                {
                    if (!String.Equals(evnt.EventType, VisitizationUtils.EventTypeCustom, StringComparison.OrdinalIgnoreCase) && evnt.UETMatchingGuid != null)
                    {
                        double goal;
                        if (midGVTable.TryGetValue(evnt.UETMatchingGuid.ToSystemGuid(), out goal))
                        {
                            evnt.GoalValue = goal;
                        }
                    }
                }
            }
        }

        private static void AddToEventGroups(Dictionary<string, List<UETEvent>> eventGroups, Guid? anid, Guid? muid, bool? isNewMuid, UETEvent currentUetEvent)
        {
            var groupCandidateKey = "UAIPId";

            // Use ANID prior to MUID
            if (anid.HasValue)
            {
                groupCandidateKey = anid.ToString();
            }
            else if (muid.HasValue)
            {
                groupCandidateKey = (isNewMuid == true) ? GroupKeyNewMuids : muid.ToString();
            }

            AddToEventGroups(eventGroups, groupCandidateKey, currentUetEvent);
        }

        /// <summary>
        /// Add the UETEvent to the dictionary.
        /// </summary>
        private static void AddToEventGroups(Dictionary<string, List<UETEvent>> eventGroups, string key, UETEvent currentUetEvent)
        {
            List<UETEvent> list;
            if (eventGroups.TryGetValue(key, out list))
            {
                list.Add(currentUetEvent);
            }
            else
            {
                eventGroups.Add(key, new List<UETEvent> { currentUetEvent });
            }
        }

    }
}
