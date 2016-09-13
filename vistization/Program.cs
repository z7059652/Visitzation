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
using VisitizationCommon;
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
            return uicRaw.MapPartitions<string>(UserIdCoverageLogProcessor.INSTANCE.GetData).Distinct();
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

        private static RDD<SerializaType> getUMS_ANIDData(RDD<SerializaType> UMS_ANIDData)
        {
            var AUMS = UMS_ANIDData.Map<SerializaType>(line =>
            {
                var aums = new UMS_ANID(line);
                return aums.SerializeObject();
            });
            return AUMS;
        }
        private static RDD<SerializaType> getUMS_MUIDData(RDD<SerializaType> UMS_MUIDData)
        {
            var MN = UMS_MUIDData.Map(line =>
            {
                var muid = new UMS_MUID(line);
                return muid.SerializeObject();
            });
            return MN;
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
            string filepath = @"hdfs:///common/vistizationData/";
            var OutputPath = @"hdfs:///user/t-zhuxia/vistizationRes/";

            string uetLogPath = filepath + "gat_20160902_0600.csv";
            var UICLogPath = filepath + "uic_20160902_0600.csv";
            string AnidPath = filepath + "ANID_20160831.csv";
            string MuidPath = filepath + "MUID_20160831.csv";
            var Visitization_AppInstall_Output = OutputPath + "Visitization_AppInstall_20160902_00";
            var NewEscrowFile = OutputPath + "NewEscrowCandidates_20160902";


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

            //appInstallVisits.Repartition(1).SaveAsTextFile(Visitization_AppInstall_Output);

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

            var VisitForUserWithTypeOfUser = getVisitsForUsersWithTypeOfUser(visitsForUsers);
            // Step 7.2: Get the ANID and MUID sub-table out of the VisitsForUsers_WithTypeOfUser because we need to update 
            // the ANID/MUID to StableIdValue according to UMS mapping
            var VisitsForUsers_WithTypeOfUser_ANID = VisitForUserWithTypeOfUser.Filter(line =>
            {
                var data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return data.TypeOfUser == 1;
            });
            var VisitsForUsers_WithTypeOfUser_MUID = VisitForUserWithTypeOfUser.Filter(line =>
            {
                var data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return data.TypeOfUser == 2;
            });
            // Step 7.3: Buid the UMS ANID/MUID view from "/shares/adCenter.BICore.SubjectArea/SubjectArea/Conversion/UMS/ANID_{yyyyMMdd}.ss(12.43GB)/MUID_{yyyyMMdd}.ss(166.66GB)" 
            var UMS_ANIDData = getDataFromFile(sc, AnidPath);
            var UMS_MUIDData = getDataFromFile(sc, MuidPath);

            // Step 7.4: Join VisitsForUsers_WithTypeOfUser_ANID(MUID) with UMS_ANID(MUID)_MappingFile to get to use the StableIdValue.
            var VisitsForUsers_WithStableIdANIDGuid = VisitsForUsers_WithTypeOfUser_ANID.Map(line=> 
            {
                var data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return data.ANID;
            });
            Console.Out.WriteLine("----------------VisitsForUsers_WithStableIdANIDGuid: " + VisitsForUsers_WithStableIdANIDGuid.Count());
            
            var VisitsForUsers_WithStableIdMUIDGuid = VisitsForUsers_WithTypeOfUser_MUID.Map(line => 
            {
                var data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return data.MUID;
            });
            Console.Out.WriteLine("----------------VisitsForUsers_WithStableIdMUIDGuid: " + VisitsForUsers_WithStableIdMUIDGuid.Count());

            var anid = getUMS_ANIDData(UMS_ANIDData).Map<KeyValuePair<Guid?, SerializaType>>(line => 
            {
                var an = line.DeserializeObject<UMS_ANID>();
                return new KeyValuePair<Guid?, SerializaType>(an.ANID,line);
            }).FlatMap<KeyValuePair<Guid?, SerializaType>>(new BroadcastJoinWrapper(VisitsForUsers_WithStableIdANIDGuid, sc).Filter);

            var muid = getUMS_MUIDData(UMS_MUIDData).Map<KeyValuePair<Guid?, SerializaType>>(line =>
            {
                var an = line.DeserializeObject<UMS_MUID>();
                return new KeyValuePair<Guid?, SerializaType>(an.MUID, line);
            }).FlatMap<KeyValuePair<Guid?, SerializaType>>(new BroadcastJoinWrapper(VisitsForUsers_WithStableIdMUIDGuid, sc).Filter);

            var VisitsForUsers_WithStableIdFromANID = VisitsForUsers_WithTypeOfUser_ANID.Map(line =>
            {
                VisitsForUser_WithTypeOfUser data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return new KeyValuePair<Guid?, SerializaType>(data.ANID, line);
            }).LeftOuterJoin(anid).Map(line => 
            {
                VisitsForUser_WithTypeOfUser data = VisitsForUser_WithTypeOfUser.Deserialize(line.Value.Item1);
                var VA = new VisitsForUsersWithStableIdFromID();
                VA.UAIPId = data.UAIPId;
                VA.TagId = data.TagId;
                VA.TagName = data.TagName;
                VA.AnalyticsGuid = data.AnalyticsGuid;
                VA.SAEventConversionFactsRow = data.SAEventConversionFactsRow;
                if (line.Value.Item2.IsDefined)
                {
                    var an = line.Value.Item2.GetValue().DeserializeObject<UMS_ANID>();
                    VA.StableId = an.ANID;
                }
                else
                    VA.StableId = data.ANID;
                return VA.SerializeObject();
            });

            var VisitsForUsers_WithStableIdFromMUID = VisitsForUsers_WithTypeOfUser_MUID.Map(line =>
            {
                VisitsForUser_WithTypeOfUser data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return new KeyValuePair<Guid?, SerializaType>(data.MUID, line);
            }).LeftOuterJoin(muid).Map(line => 
            {
                VisitsForUser_WithTypeOfUser data = VisitsForUser_WithTypeOfUser.Deserialize(line.Value.Item1);
                var VA = new VisitsForUsersWithStableIdFromID();
                VA.UAIPId = data.UAIPId;
                VA.TagId = data.TagId;
                VA.TagName = data.TagName;
                VA.AnalyticsGuid = data.AnalyticsGuid;
                VA.SAEventConversionFactsRow = data.SAEventConversionFactsRow;
                if (line.Value.Item2.IsDefined)
                {
                    var an = line.Value.Item2.GetValue().DeserializeObject<UMS_MUID>();
                    VA.StableId = an.MUID;
                }
                else
                    VA.StableId = data.MUID;
                return VA.SerializeObject();
            });

            Console.WriteLine("-----------------VisitsForUsers_WithStableIdFromANID: " + VisitsForUsers_WithStableIdFromANID.Count());
            Console.WriteLine("-----------------VisitsForUsers_WithStableIdFromMUID: " + VisitsForUsers_WithStableIdFromMUID.Count());          

            // Step 7.5: Select the UETUserId from the StableId and add the UserType according to whether it is from ANID or MUID
            var VisitsForUsers_WithUETUserId_MUID_ANID_UNION_Part1 = VisitsForUsers_WithStableIdFromANID.Map(line =>
            {
                var VA = line.DeserializeObject<VisitsForUsersWithStableIdFromID>();
                VisitsForUsersWithUETUserIdMUIDANIDPart data = new VisitsForUsersWithUETUserIdMUIDANIDPart();
                data.UETUserId = VA.StableId;
                data.TypeOfUser = UserType.A;
                data.UAIPId = VA.UAIPId;
                data.TagId = VA.TagId;
                data.TagName = VA.TagName;
                data.AnalyticsGuid = VA.AnalyticsGuid;
                data.SAEventConversionFactsRow = VA.SAEventConversionFactsRow;
                return data.SerializeObject();
            }); 
            var VisitsForUsers_WithUETUserId_MUID_ANID_UNION_Part2 = VisitsForUsers_WithStableIdFromMUID.Map(line =>
            {
                var VA = line.DeserializeObject<VisitsForUsersWithStableIdFromID>();
                VisitsForUsersWithUETUserIdMUIDANIDPart data = new VisitsForUsersWithUETUserIdMUIDANIDPart();
                data.UETUserId = VA.StableId;
                data.TypeOfUser = UserType.M;
                data.UAIPId = VA.UAIPId;
                data.TagId = VA.TagId;
                data.TagName = VA.TagName;
                data.AnalyticsGuid = VA.AnalyticsGuid;
                data.SAEventConversionFactsRow = VA.SAEventConversionFactsRow;
                return data.SerializeObject();
            });
            var VisitsForUsers_WithUETUserId_MUID_ANID_UNION_Part = VisitsForUsers_WithUETUserId_MUID_ANID_UNION_Part2.Union(VisitsForUsers_WithUETUserId_MUID_ANID_UNION_Part1);


            // Step 7.6: For the AnalyticsGuid sub-table of the VisitsForUsers_WithTypeOfUser, use AnalyticsGuid as the UETUserId and "AG" as the UserType.
            var VisitsForUsers_WithUETUserId_AnalyticsGuid_Other_UNION_Part = VisitForUserWithTypeOfUser.Filter(line =>
            {
                var data = VisitsForUser_WithTypeOfUser.Deserialize(line);
                return data.TypeOfUser == 3 || data.TypeOfUser == -1;
            }).Map(line => 
            {
                var Visits = VisitsForUser_WithTypeOfUser.Deserialize(line);
                VisitsForUsersWithUETUserIdMUIDANIDPart data = new VisitsForUsersWithUETUserIdMUIDANIDPart();
                data.UAIPId = Visits.UAIPId;
                data.TagId = Visits.TagId;
                data.TagName = Visits.TagName;
                data.AnalyticsGuid = Visits.AnalyticsGuid;
                data.SAEventConversionFactsRow = Visits.SAEventConversionFactsRow;
                if (Visits.TypeOfUser == 3)
                {
                    data.UETUserId = Visits.AnalyticsGuid;
                    data.TypeOfUser = UserType.AG;
                }
                else
                {
                    data.UETUserId = Visits.UAIPId;
                    data.TypeOfUser = UserType.UA;
                }
                return data.SerializeObject();
            });
            // Step 7.7: Union result from 7.5 and 7.6
            var VisitsForUsers_WithUETUserId = VisitsForUsers_WithUETUserId_MUID_ANID_UNION_Part.Union(VisitsForUsers_WithUETUserId_AnalyticsGuid_Other_UNION_Part);

            // Step 7.8: Reduce on UETUserId, UAIPId, TagId, using UserCombineReducer
            VisitsForUsers_WithUETUserId = VisitsForUsers_WithUETUserId.Map(line => 
            {
                var data = line.DeserializeObject<VisitsForUsersWithUETUserIdMUIDANIDPart>();
                return new VisitsForUsersWithUETUserId(data, data.SAEventConversionFactsRow.Visits[0].Events[0].EventDateTime).SerializeObject();
            });

            var VisitsForUsers_Current = VisitsForUsers_WithUETUserId
            .Map(line =>
            {
                var data = line.DeserializeObject<VisitsForUsersWithUETUserId>();
                return new KeyValuePair<long, string>(data.EventDateTime, line);
            })
            .SortByKey()
            .Map(line =>
            {
                var data = line.Value.DeserializeObject<VisitsForUsersWithUETUserId>();
                var key = string.Format("{0},{1},{2}", data.UETUserId, data.UAIPId, data.TagId);
                return new KeyValuePair<string, string>(key, line.Value);
            })
            .ReduceByKey((x, y) => 
            {
                if (!string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y))
                    return x + delimeter + y;
                if (!string.IsNullOrEmpty(x))
                    return x;
                if (!string.IsNullOrEmpty(y))
                    return y;
                return null;
            }).Map<SerializaType>(UserCombineReducer.INSTANCE.getData);
            
            // Step 8: Handle the current hour result with Escrow visits from the previous hour:
            //As EscrowFile doesn't exists, so skip this step

            // Step 9: Calculate conversions for each visit using GoalConversionProcessor and output it.
            var VisitsWithConversions = VisitsForUsers_Current.MapPartitions(GoalConversionProcessor.INSTANCE.getData);

            // Step 10: Update the Escrow file
            var VisitsWithConversions_notUAIP = VisitsWithConversions.Filter(line => 
            {
                var data = line.DeserializeObject<VisitsWithConversion>();
                return data.SAEventConversionFactsRow.UserIdType != UETUserIdType.UAIPID;
            });

            var NewEscrowCandidates = VisitsWithConversions_notUAIP.MapPartitions(EscrowCandidateProcessor.INSTANCE.getData);
            // Step 10.2: Output the result to the new escrow file
            NewEscrowCandidates.Repartition(1).SaveAsTextFile(NewEscrowFile);
            return;
        }
    }
}
