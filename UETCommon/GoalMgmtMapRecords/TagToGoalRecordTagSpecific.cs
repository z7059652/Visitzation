using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// A row in TagSpecificGoalType Tag-to-Goal Map file
    /// </summary>
    public class TagToGoalRecordTagSpecific : TagToGoalRecord
    {
        public TagToGoalRecordTagSpecific() { GoalType = GoalType.TagSpecific; }

        public TagToGoalRecordTagSpecific(Dictionary<string, int> columnMetadata, object[] row)
            : base(columnMetadata, row)
        {
            GoalType = GoalType.TagSpecific;
        }

        public override List<Conversion> FindConversionsIn(Visit visit, out bool isAchieved)
        {
            isAchieved = false;
            List<Conversion> conversions = null;

            if (visit != null && visit.Events != null)
            {
                int evntIndex = -1;

                foreach (var evnt in visit.Events)
                {
                    // TODO: Drop the EventsIndex32 and EventsIndex properties. They are not used anywhere. And we are putting a List of int for it?
                    evntIndex++;
                    if (evnt.customEvent == null)
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
                var properties = new object[7];
                base.Properties.CopyTo(properties, 0);
                return properties;
            }
        }
        #endregion
    }
}
