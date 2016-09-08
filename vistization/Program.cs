using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Streaming;
using Microsoft.AdCenter.BI.UET.Schema;
using Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon;
using Microsoft.AdCenter.BI.UET.StreamingSchema;
using Microsoft.BI.Common.CryptoUtils;

using SerializaType = System.String;

namespace VisitizationStreaming
{
    class Program
    {
        private const char delimeter = '\n';
        private static RDD<string> getDataFromFile(SparkContext sc, string filename)
        {
            return (sc.TextFile(filename)).Filter(line => { return !line.StartsWith("#"); });
        }
        private static RDD<string> getUICData(RDD<string> uicRaw)
        {
            return uicRaw.Map<string>(UserIdCoverageLogProcessor.INSTANCE.GetData).Distinct();
        }
        private static RDD<string> getUMSData(RDD<string> umsRaw)
        {
            return umsRaw;
        }
        private static RDD<SerializaType> getUETLogs(RDD<SerializaType> rawUetLogs)
        {
            var UetLogs = rawUetLogs.MapPartitions<string>(UETLogProcessor.INSTANCE.GetData);
            return UetLogs.Filter(line => !string.IsNullOrEmpty(line));
        }

        private static RDD<SerializaType> getVisitsForUsersWithTypeOfUser(RDD<SerializaType> VisitsForUsers)
        {
            var Visits = VisitsForUsers.Map<SerializaType>(line=>
            {
                VisitizationSchema data = VisitizationSchema.Deserialize(line);
                VisitsForUser_WithTypeOfUser Vfu = new VisitsForUser_WithTypeOfUser();
                Vfu.UAIPId = data.UAIPId;
                Vfu.TagId = data.TagId;
                Vfu.AnalyticsGuid = data.AnalyticsGuid;
                Vfu.SAEventConversionFactsRow = data.SAEventConversionFactsRow;
                if(Vfu.SAEventConversionFactsRow.ANID != null)
                {
                    Vfu.ANID = Vfu.SAEventConversionFactsRow.ANID.ToSystemGuid();
                }
                if(Vfu.SAEventConversionFactsRow.MUID != null)
                {
                    Vfu.MUID = Vfu.SAEventConversionFactsRow.MUID.ToSystemGuid();
                }
                if(Vfu.SAEventConversionFactsRow.MUID != null && Vfu.SAEventConversionFactsRow.IsNewMUID != true)
                {
                    Vfu.TypeOfUser = 2;
                }
                else 
                {
                    if (Vfu.SAEventConversionFactsRow.ANID != null)
                    {
                        Vfu.TypeOfUser = 1;
                    }
                    else
                    {
                        if (Vfu.AnalyticsGuid != null)
                            Vfu.TypeOfUser = 3;
                    }
                }
                return VisitsForUser_WithTypeOfUser.Serialize(Vfu);
            });

            return Visits;
        }
        private static void test(RDD<SerializaType> data)
        {
            Console.WriteLine("---------------sample data start --------------------");
            foreach(var y in data.TakeSample(false,5,12))
            {
                Console.WriteLine(y);
            }
            Console.WriteLine("---------------sample data end --------------------");

        }
        //private static RDD<string> getUserVisit
        static void Main(string[] args)
        {
            /*Loc*/
            //LocalTest lt = new LocalTest();
            //lt.test();
            //return;
            ///
            string filepath = @"hdfs:///common/vistizationData/";
            var OutputPath = @"hdfs:///user/t-zhuxia/vistizationRes/";

            string uetLogPath = filepath + "gat_20160902_0600.csv";
            var UICLogPath = filepath + "uic_20160902_0600.csv";
            string AnidPath = filepath + "ANID_20160831.csv";
            string MuidPath = filepath + "MUID_20160831.csv";

            SparkConf conf = (new SparkConf()).SetAppName("VisitizationStreaming");
            SparkContext sc = new SparkContext(conf);

            RDD<string> rawUetLogs = getDataFromFile(sc,uetLogPath);

            var uetLogs = getUETLogs(rawUetLogs);
            
            var uetLogsKeyValpair = uetLogs.Map(line =>
            {
                if(!string.IsNullOrEmpty(line))
                {
                    UETLogView data = UETLogView.Deserialize(line);
                    string key = data.DedupKey + "," + 
                                 data.ANID + "," + 
                                 data.IsNewMUID + "," +
                                 data.UAIPId + "," +
                                 data.ReferrerURL + ","+
                                 data.QueryString + "," + 
                                 data.AnalyticsGuid;
                    return new KeyValuePair<string, string>(key, line);
                }
                return new KeyValuePair<string,string>(null,null);
            });

            uetLogs = uetLogsKeyValpair.ReduceByKey((x, y) => 
            { 
                if(!string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y))
                    return x + delimeter + y;
                if (!string.IsNullOrEmpty(x))
                    return x;
                if (!string.IsNullOrEmpty(y))
                    return y;
                return null;
            }).Map<string>(UETLogDedupReducer.INSTANCE.GetData).Filter(line => !string.IsNullOrEmpty(line));

/*****************************************to do after this ****************************************************/
            var uetLogs_PageVisit = uetLogs.Filter(line =>
            {
                UETLogView data = UETLogView.Deserialize(line);
                return string.IsNullOrEmpty(data.AppInstallClickId);
            });
            
            Console.Out.WriteLine("----------------uetLogs_PageVisitCount: " + uetLogs_PageVisit.Count());
            
            var uetLogs_AppInstall = uetLogs.Filter(line =>
            {
                UETLogView data = UETLogView.Deserialize(line);
                return (!string.IsNullOrEmpty(data.AppInstallClickId));
            });
            RDD<string> appInstallVisits = uetLogs_AppInstall.Map<string>(AppInstallProcessor.INSTANCE.GetData);

            Console.Out.WriteLine("----------------appInstallVisitsCount: " + appInstallVisits.Count());

            var Visitization_AppInstall_Output = OutputPath + "Visitization_AppInstall_20160902_00";
            appInstallVisits.Repartition(1).SaveAsTextFile(Visitization_AppInstall_Output);


            //----- Get UIC log
            var uicRaw = getDataFromFile(sc, UICLogPath);
            var UserIdConverage = getUICData(uicRaw);

            //----- Join uetlog with uic log
            var uetColumns = uetLogs_PageVisit.Map(line =>
            {
                var uetLog = UETLogView.Deserialize(line);
                return new KeyValuePair<Guid?, string>(uetLog.UETMatchingGuid, line);
            });

            var uicColumns = UserIdConverage.Map(line =>
            {
                var uic = UserIdCoverageShcema.Deserialize(line);
                return new KeyValuePair<Guid?, Guid?>(uic.UETMatchingGuid, uic.AnalyticsGuid);
            });

            var UETLogProcessedEntriesPageVisit = uetColumns.LeftOuterJoin(uicColumns).Map(line =>
            {
                var value = UETLogView.Deserialize(line.Value.Item1);
                if(line.Value.Item2.IsDefined)
                {
                    var agid = line.Value.Item2.GetValue();
                    if (agid != null)
                    {
                        value.AnalyticsGuid = agid;
                    }
                    value.DedupKey = null;
                    value.QueryString = null;
                }
                return UETLogView.Serialize(value);
            });

            var visitsForUsersKeyValuePair = UETLogProcessedEntriesPageVisit.Map(line =>
            {
                var value = UETLogView.Deserialize(line);
                var key = value.UAIPId.ToString() + ","+value.TagId.ToString();
                return new KeyValuePair<string, string>(key, line);
            }).ReduceByKey((x, y) => { return x + delimeter + y; });

            var visitsForUsers = visitsForUsersKeyValuePair.FlatMap<string>(line => 
            {
                return VisitizeReducer.INSTANCE.GetData(line); 
            });

            // Step 7: First field to fill is UserIdType and build the general "UETUserId", by default it is UAIPID during the construction of SAEventConversionFacts. 
            // Step 7.1: Build the TypeOfUser field.
            // The way of deciding the TypeOfUser is:
            // 1. If MUID is not NULL and IsNewMUID is false, UserIdType is MUID (TypeOfUser 2), later will join with UMS MUID view.
            // 2. If MUID is NULL but ANID is not, UserIdType is ANID (TypeOfUser 1), ater will join with UMS ANID view.
            // 3. If both MUID and ANID are NULL, but AnalyticsGuid is nut NULL, UserIdType is AnalyticsGuid (TypeOfUser 3)
            // 4. If AnalyticsGuid is also NULL, UserIdType is Unknown (TypeOfUser -1)



            //----- Output AppInstallVisits to AppInstallVisits output file.
            var Visitization_Output = OutputPath + "Visitization_201607301000";

            var array = visitsForUsers.Repartition(1).Collect();
            Console.WriteLine("visitsForUsersCount: " + array.Count());          
        }
    }
}
