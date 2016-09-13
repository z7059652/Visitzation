using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.AdCenter.BI.UET.StreamingSchema;
using Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerializeType = System.String;

namespace VisitizationCommon
{
    public class EscrowCandidateProcessor
    {
        private static EscrowCandidateProcessor instance = null;
        public static EscrowCandidateProcessor INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new EscrowCandidateProcessor();
                return instance;
            }
        }

        public IEnumerable<SerializeType> getData(IEnumerable<SerializeType> data)
        {
            var sessionTimeoutThreshold = VisitizationUtils.GetTickesFromMinutes("30");     // Current input value is 30 minutes
            var endOfDelta = DateTime.Parse(@"2016-09-02 07:00:00").Ticks;
            var maxVisitDuration = VisitizationUtils.GetTicksFromHours("24");               // Current input value is 24 hours.

            List<SerializeType> res = new List<SerializeType>();
            foreach(var line in data)
            {
                var input = line.DeserializeObject<VisitsWithConversion>();
                var output = new NewEscrowCandidate();
                var fact = input.SAEventConversionFactsRow;
                var maxEventDateTime = fact.Visits.Max(visit => visit.Events[visit.Events.Count - 1].EventDateTime);
                var lastVisit = fact.Visits.First(visit => visit.Events[visit.Events.Count - 1].EventDateTime == maxEventDateTime);
                if (lastVisit.Events.Count > 0)
                {
                    var lastVisitStartDateTime = lastVisit.Statistic != null ? lastVisit.Statistic.VisitStartDateTime : lastVisit.Events[0].EventDateTime;
                    var lastVisitDuration = endOfDelta - lastVisitStartDateTime;

                    // Check whether the visit duration exceeds the maximum allowed duration
                    if (lastVisitDuration <= maxVisitDuration)
                    {
                        var lastEvent = lastVisit.Events[lastVisit.Events.Count - 1];

                        // Check if the last event is close enough to the end of the current delta
                        if (endOfDelta - lastEvent.EventDateTime <= sessionTimeoutThreshold)
                        {
                            // Add this row to the escrow file
                            output.UETUserId = (Guid)input.UETUserId;
                            output.UAIPId = input.UAIPId;
                            output.AnalyticsGuid = input.AnalyticsGuid;
                            output.TagId = input.TagId;
                            output.TagName = input.TagName;
                            var escrowFact = new EscrowVisitRecord();
                            escrowFact.LastEventDateTime = lastEvent.EventDateTime;
                            escrowFact.LastEventIsNewMUID = lastEvent.IsNewMUID;
                            escrowFact.LastEventReferrerURL = lastEvent.ReferrerURL;
                            escrowFact.Statistic = lastVisit.Statistic;
                            escrowFact.VisitId = lastVisit.VisitId;
                            escrowFact.HasConversions = lastVisit.Conversions != null && lastVisit.Conversions.Count > 0;
                            output.EscrowRow = escrowFact;
                            res.Add(output.SerializeObject());
                        }
                    }
                }
            }
            return res;
        }
    }
}
