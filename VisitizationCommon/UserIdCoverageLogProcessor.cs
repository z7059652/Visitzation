using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.AdCenter.BI.UET.StreamingSchema;
using Microsoft.AdCenter.BI.UET.Common.Helpers;


namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    [Serializable]
    public class UserIdCoverageLogProcessor
    {
        private static UserIdCoverageLogProcessor instance = null;
        public static UserIdCoverageLogProcessor INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new UserIdCoverageLogProcessor();
                return instance;
            }
        }

        private UserIdCoverageLogProcessor()
        {

        }

        public IEnumerable<string> GetData(IEnumerable<string> data)
        {
            List<string> res = new List<string>();
            AnalyticsGuidExtractor analyticsGuidExtractor = new AnalyticsGuidExtractor();
            BondReader<UserIdCoveragePair> reader = new BondReader<UserIdCoveragePair>();
            foreach(var line in data)
            {
                if (string.IsNullOrEmpty(line))
                    return null;
                string[] values = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if(values == null || values.Length <3)
                    return null;
                byte[] LogRecord = Convert.FromBase64String(values[0]);

                UserIdCoveragePair uicPair;
                if(reader.TryParse(LogRecord,out uicPair))
                {
                    if(uicPair != null && !String.IsNullOrEmpty(uicPair.AnalyticsCookie) && !String.IsNullOrEmpty(uicPair.UETMatchingQueryString))
                    {
                        Guid? analyticsGuid;
                        if (analyticsGuidExtractor.TryExtractAnalyticsGuid(uicPair.AnalyticsCookie, out analyticsGuid))
                        {
                            var mid = uicPair.UETMatchingQueryString.Split('&').FirstOrDefault(s => s.StartsWith("mid="));
                            if(mid != null)
                            {
                                var uetMatchingGuid = CommonUtils.ParseGuid(mid.Substring(4));
                                if(uetMatchingGuid.HasValue)
                                {
                                    var output = new UserIdCoverageShcema();
                                    output.UETMatchingGuid = uetMatchingGuid;
                                    output.AnalyticsGuid = analyticsGuid;
                                    res.Add(UserIdCoverageShcema.Serialize(output));
                                }
                            }
                        }
                    }
                }
            }
            return res;
        }
    }
}
