using System;
using System.Collections.Generic;
using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.AdCenter.BI.UET.StreamingSchema;
using Microsoft.Bond;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    public class AppInstallProcessor
    {
        private static AppInstallProcessor instance = null;
        public static AppInstallProcessor INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new AppInstallProcessor();
                return instance;
            }
        }
        private AppInstallProcessor()
        {

        }
        public string GetData(string line)
        {
            if(string.IsNullOrEmpty(line))
            {
                return null;
            }
            else
            {
                AppInstallVisitsSchema output = new AppInstallVisitsSchema();
                UETLogView data = UETLogView.Deserialize(line);
                output.AppInstallClickId = data.AppInstallClickId;

                var eventDataTime = data.EventDateTime;
                var anid = data.ANID;
                var muid = data.MUID;
                var referrerUrl = data.ReferrerURL;

                var visit = new Visit();
                visit.VisitId = VisitizationUtils.GenerateVisitId(anid, muid, eventDataTime, referrerUrl);

                var uetEvent = new UETEvent();
                uetEvent.EventDateTime = eventDataTime;
                uetEvent.ANID = anid == null ? null : new GUID(anid.Value);
                uetEvent.MUID = muid == null ? null : new GUID(muid.Value);
                uetEvent.ReferrerURL = referrerUrl;
                uetEvent.PageTitle = data.PageTitle;
                uetEvent.NavigatedFromURL = data.NavigatedFromURL;
                uetEvent.GoalValue = data.GoalValue;
                uetEvent.PageLoad = data.PageLoad;
                uetEvent.Version = data.Version;
                uetEvent.LogServerName = data.LogServerName;
                uetEvent.EventType = data.EventType;

                visit.Events = new List<UETEvent> { uetEvent };

                visit.Statistic = new VisitStatistic { VisitStartDateTime = eventDataTime };
                output.Visit = visit;
                return AppInstallVisitsSchema.Serialize(output);
            }
        }
    }
}
