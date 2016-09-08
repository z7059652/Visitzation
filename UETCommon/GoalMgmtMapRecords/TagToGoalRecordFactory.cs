using System.Collections.Generic;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// TagToGoalRecord Factory.
    /// </summary>
    public static class TagToGoalRecordFactory
    {
        /// <summary>
        /// Create corresponding TagToGoalRecord according to the passed in goalType.
        /// </summary>
        public static TagToGoalRecord CreateGoal(GoalType goalType, Dictionary<string, int> columnMetadata, object[] row)
        {
            TagToGoalRecord tagToGoalRecord;
            switch (goalType)
            {
                case GoalType.DestinationUrl:
                    tagToGoalRecord = new TagToGoalRecordDestinationUrl(columnMetadata, row);
                    break;
                case GoalType.DurationPerVisit:
                    tagToGoalRecord = new TagToGoalRecordDurationPerVisit(columnMetadata, row);
                    break;
                case GoalType.PagesPerVisit:
                    tagToGoalRecord = new TagToGoalRecordPagesPerVisit(columnMetadata, row);
                    break;
                case GoalType.CustomEvent:
                    tagToGoalRecord = new TagToGoalRecordCustomEvent(columnMetadata, row);
                    break;
                case GoalType.TagSpecific:
                    tagToGoalRecord = new TagToGoalRecordTagSpecific(columnMetadata, row);
                    break;
                default:
                    tagToGoalRecord = null;
                    break;
            }

            return tagToGoalRecord;
        }
    }
}
