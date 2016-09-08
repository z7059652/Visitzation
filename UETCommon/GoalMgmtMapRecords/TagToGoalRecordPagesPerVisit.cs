using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// A row in PagesPerVisitGoalType Tag-to-Goal Map file
    /// </summary>
    public class TagToGoalRecordPagesPerVisit : TagToGoalRecord
    {
        private static int _numberOfPagesOrdinal = -1;
        private static int _comparisonOperatorOrdinal = -1;
        public GoalProperty<short> NumberOfPagesProperty;

        public TagToGoalRecordPagesPerVisit() { GoalType = GoalType.PagesPerVisit; }

        public TagToGoalRecordPagesPerVisit(IReadOnlyDictionary<string, int> columnMetadata, IList<object> row)
            : base(columnMetadata, row)
        {
            GoalType = GoalType.PagesPerVisit;

            if (_numberOfPagesOrdinal == -1)
            {
                _numberOfPagesOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "PageVisitGoal");
            }

            if (_comparisonOperatorOrdinal == -1)
            {
                _comparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "PageVisitOperator");
            }

            var numberOfPages = (short)row[_numberOfPagesOrdinal];
            var valueOperatorByte = (byte)row[_comparisonOperatorOrdinal];

            if (!GoalUtils.IsValidValueOperator(valueOperatorByte))
            {
                throw new Exception(String.Format("NumberOfPages comparison operator value {0} is outside of range.", valueOperatorByte));
            }

            var valueComparisonOperator = (ValueComparisonOperator)valueOperatorByte;

            if (!GoalUtils.IsValidNumericOperator(valueComparisonOperator))
            {
                throw new Exception(String.Format("Value operator value {0} is not supported by {1} goal type.", valueComparisonOperator.ToString(), GoalType));
            }

            NumberOfPagesProperty = new GoalProperty<short>
            {
                ValueComparisionOperator = valueComparisonOperator,
                PropertyValue = numberOfPages
            };
        }

        public override List<Conversion> FindConversionsIn(Visit visit, out bool isAchieved)
        {
            isAchieved = false;
            List<Conversion> conversions = null;

            if (visit != null && visit.Events != null && visit.Events.Count > 0 &&
                visit.Statistic != null && 
                (visit.Statistic.AchievedGoals == null ||
                 !visit.Statistic.AchievedGoals.Contains(GoalId))) // Pages Per Visit goal can be achieved only once
            {
                var numberOfPages = visit.Statistic.NumberOfPages;
                var referralPage = string.Empty;

                // For each page_load event (evnt.customEvent == null)
                // TODO: Drop the EventsIndex32 and EventsIndex properties. They are not used anywhere. And we are putting a List of int for it?
                int evntIndex = -1;
                foreach (var evnt in visit.Events)
                {
                    evntIndex++;

                    if (evnt.customEvent == null)
                    {
                        if (!String.Equals(evnt.ReferrerURL, referralPage, StringComparison.OrdinalIgnoreCase))
                        {
                            // Don't count if the same page was re-loaded
                            referralPage = evnt.ReferrerURL;
                            numberOfPages++;
                        }

                        // Convert numberOfPages to short to make it compatible with the goal schema.
                        var isConversion = GoalUtils.CompareNumericProperty(NumberOfPagesProperty, (short)numberOfPages);
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
                            isAchieved = true; 
                            break;
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
                properties[7] = NumberOfPagesProperty.PropertyValue;
                properties[8] = NumberOfPagesProperty.ValueComparisionOperator;
                return properties;
            }
        }
        #endregion
    }
}
