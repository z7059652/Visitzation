using System;
using System.Collections.Generic;
using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// A row in DurationPerVisitGoalType Tag-to-Goal Map file
    /// </summary>
    public class TagToGoalRecordDurationPerVisit : TagToGoalRecord
    {
        private static int _numberOfSecondsOrdinal = -1;
        private static int _comparisonOperatorOrdinal = -1;
        public GoalProperty<int> NumberOfSecondsProperty;

        public TagToGoalRecordDurationPerVisit() { GoalType = GoalType.DurationPerVisit; }

        public TagToGoalRecordDurationPerVisit(IReadOnlyDictionary<string, int> columnMetadata, IList<object> row)
            : base(columnMetadata, row)
        {
            GoalType = GoalType.DurationPerVisit;

            if (_numberOfSecondsOrdinal == -1)
            {
                _numberOfSecondsOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "DurationGoal");
            }

            if (_comparisonOperatorOrdinal == -1)
            {
                _comparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "DurationOperator");
            }

            var numberOfSeconds = (int)row[_numberOfSecondsOrdinal];
            var valueOperatorByte = (byte)row[_comparisonOperatorOrdinal];
            if (!GoalUtils.IsValidValueOperator(valueOperatorByte))
            {
                throw new Exception(String.Format("NumberOfSeconds comparison operator value {0} is outside of range.", valueOperatorByte));
            }

            var valueComparisonOperator = (ValueComparisonOperator)valueOperatorByte;

            if (!GoalUtils.IsValidNumericOperator(valueComparisonOperator))
            {
                throw new Exception(String.Format("Value operator value {0} is not supported by {1} goal type.", valueComparisonOperator.ToString(), GoalType));
            }

            NumberOfSecondsProperty = new GoalProperty<int>
            {
                ValueComparisionOperator = valueComparisonOperator,
                PropertyValue = numberOfSeconds
            };
        }

        public override List<Conversion> FindConversionsIn(Visit visit, out bool isAchieved)
        {
            isAchieved = false;
            List<Conversion> conversions = null;

            if (visit != null && visit.Events != null && visit.Events.Count > 0 &&
                visit.Statistic != null &&
                (visit.Statistic.AchievedGoals == null ||
                 !visit.Statistic.AchievedGoals.Contains(GoalId)))  // Duration Per Visit goal can be achieved only once
            {
                // TODO: Drop the EventsIndex32 and EventsIndex properties. They are not used anywhere. And we are putting a List of int for it?
                int evntIndex = -1;
                var visitStartDateTime = visit.Statistic.VisitStartDateTime;

                foreach (var evnt in visit.Events)
                {
                    evntIndex++;
                    var elapsedTicks = evnt.EventDateTime - visitStartDateTime;
                    var elapsedSpan = new TimeSpan(elapsedTicks);

                    var isConversion = GoalUtils.CompareNumericProperty(NumberOfSecondsProperty, (int)elapsedSpan.TotalSeconds);
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
                properties[7] = NumberOfSecondsProperty.PropertyValue;
                properties[8] = NumberOfSecondsProperty.ValueComparisionOperator;
                return properties;
            }
        }
        #endregion
    }
}
