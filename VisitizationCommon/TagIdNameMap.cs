using Microsoft.BI.Common.IO;
using Microsoft.BI.Common.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    [Serializable]
    public class TagIdNameMap
    {
        public Dictionary<int, string> IdToNameMap
        {
            get;
            private set;
        }

        // TagName-to-Dictionary{CustomerId, TagId} map.
        public Dictionary<string, Dictionary<int, int>> NameToIdMap
        {
            get;
            private set;
        }

        public TagIdNameMap()
        {
            IdToNameMap = new Dictionary<int, string>();
            NameToIdMap = new Dictionary<string, Dictionary<int, int>>();

            var mapFileName = String.Format("Stream_AllPipe_CustomerTagMap.dts.gz");
            using (var fileStream = new FileStream(mapFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    using (var dtsReader = new DtsReader(zipStream))
                    {
                        var schema = DsvUtilities.GetDsvTable("StripeMapFiles__15_3.dsv", "CustomerTagMap_Stripe");
                        var columnMetadata = MapFileUtils.ValidateSchema(schema, dtsReader);

                        foreach (var line in dtsReader.ReadAllLines())
                        {
                            var record = new TagToCustomerRecord(columnMetadata, line);

                            if (!IdToNameMap.ContainsKey(record.TagId))
                            {
                                IdToNameMap.Add(record.TagId, record.TagName);
                            }

                            Dictionary<int, int> customerIdToTagId;
                            if (!NameToIdMap.TryGetValue(record.TagName, out customerIdToTagId))
                            {
                                customerIdToTagId = new Dictionary<int, int>();
                                customerIdToTagId.Add(record.CustomerId, record.TagId);

                                NameToIdMap.Add(record.TagName, customerIdToTagId);
                            }
                            else if (!customerIdToTagId.ContainsKey(record.CustomerId))
                            {
                                customerIdToTagId.Add(record.CustomerId, record.TagId);
                            }
                        }
                    }
                }
            }
        }
    }
}
