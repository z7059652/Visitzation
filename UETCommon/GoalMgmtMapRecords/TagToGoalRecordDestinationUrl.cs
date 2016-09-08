using System;
using System.Collections.Generic;
using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// A row in DestinationUrlGoalType Tag-to-Goal Map file
    /// </summary>
    public class TagToGoalRecordDestinationUrl : TagToGoalRecord
    {
        private static int _destinationUrlOrdinal = -1;
        private static int _comparisonOperatorOrdinal = -1;
        public GoalProperty<string> DestinationUrlProperty;

        public TagToGoalRecordDestinationUrl() { GoalType = GoalType.DestinationUrl; }

        public TagToGoalRecordDestinationUrl(IReadOnlyDictionary<string, int> columnMetadata, IList<object> row)
            : base(columnMetadata, row)
        {
            GoalType = GoalType.DestinationUrl;

            if (_destinationUrlOrdinal == -1)
            {
                _destinationUrlOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "DestinationURL");
            }

            if (_comparisonOperatorOrdinal == -1)
            {
                _comparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "DestinationURLOperator");
            }

            var destinationUrl = (string)row[_destinationUrlOrdinal];
            var comparisonOperatorByte = (byte) row[_comparisonOperatorOrdinal];
            if (!GoalUtils.IsValidComparisonOperator(comparisonOperatorByte))
            {
                throw new Exception(String.Format("DestinationUrl comparison operator value {0} is outside of range.", comparisonOperatorByte));
            }

            var comparisonOperator = (GoalComparisonOperator) comparisonOperatorByte;

            if (!GoalUtils.IsValidStringOperator(comparisonOperator))
            {
                throw new Exception(String.Format("Comparison operator value {0} is not supported by {1} goal type.", comparisonOperator.ToString(), GoalType));
            }

            DestinationUrlProperty = new GoalProperty<string>
            {
                ComparisonOperator = comparisonOperator,
                PropertyValue = destinationUrl.Trim()
            };
        }

        public override List<Conversion> FindConversionsIn(Visit visit, out bool isAchieved)
        {
            isAchieved = false; // Always false because Destination URL goal can be achieved multiple times per visit
            List<Conversion> conversions = null;

            if (visit != null && visit.Events != null && visit.Events.Count > 0)
            {
                // TODO: Drop the EventsIndex32 and EventsIndex properties. They are not used anywhere. And we are putting a List of int for it?
                int evntIndex = -1;

                // For each page_load event (evnt.customEvent == null)
                foreach (var evnt in visit.Events)
                {
                    evntIndex++;

                    // Skip custom events
                    if (evnt.customEvent == null)
                    {
                        var isConversion = GoalUtils.CompareStringProperty(DestinationUrlProperty, evnt.ReferrerURL, compareUrls: true);
                        if (isConversion)
                        {
                            double? goalValue = FetchGoalValue(GoalValue, evnt.GoalValue, GoalValueSourceId);
                            var conversion = new Conversion
                            {
                                GoalId = GoalId,
                                GoalLookbackWindow = LookbackWindow,
                                GoalValue = goalValue,
                                EventsIndex32 = new List<int> { evntIndex },
                                ConversionDateTime = evnt.EventDateTime,
                                GoalTrackingType = GoalTrackingType,
                                AccountId = AccountId,
                                GoalValueSourceId = GoalValueSourceId
                            };

                            if (conversions == null)
                            {
                                conversions = new List<Conversion>();
                            }

                            conversions.Add(conversion);
                        }
                    }
                }
            }

            return conversions;
        }

        #region OnlyUsedInTests
        /// <summary>
        /// Collection of properties is used to populate Goal-To-Tag Test map file. 
        /// Order of properties should match to the map file schema.
        /// </summary>
        public override object[] Properties 
        {
            get
            {
                var properties = new object[9];
                base.Properties.CopyTo(properties, 0);
                properties[7] = DestinationUrlProperty.PropertyValue;
                properties[8] = DestinationUrlProperty.ComparisonOperator;
                return properties;
            }
        }
        #endregion
    }
}
