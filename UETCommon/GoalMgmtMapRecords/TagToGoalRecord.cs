using System;
using System.Collections.Generic;
using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// The class contains common properties across all goal types
    /// </summary>
    public abstract class TagToGoalRecord
    {
        // The following 7 ordinal variables will be shared across objects of all the subclasses
        private static int _tagIdOrdinal = -1;
        private static int _goalIdOrdinal = -1;
        private static int _lookbackWindowOrdinal = -1;
        private static int _goalValue = -1;
        private static int _accountIdOrdinal = -1;
        private static int _goalTrackingType = -1;
        private static int _goalValueSourceIdOrdinal = -1;

        // Column name to column index mapping
        public int TagId { get; set; }
        public int GoalId { get; set; }
        public int LookbackWindow { get; set; }
        public double? GoalValue { get; set; }
        public GoalType GoalType { get; protected set; }
        public short GoalTrackingType { get; set; }
        public int? AccountId { get; set; }
        public short GoalValueSourceId { get; set; }

        public enum GoalValueSource
        {
            UserInterface = 1,
            DynamicGoalValue = 2,
            IgnoreGoalValue = 3
        }


        protected TagToGoalRecord() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="columnMetadata"> Column name to column index mapping. </param>
        /// <param name="row"></param>
        protected TagToGoalRecord(IReadOnlyDictionary<string, int> columnMetadata, IList<object> row)
        {
            if (_tagIdOrdinal == -1)
            {
                _tagIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "TagId");
            }

            if (_goalIdOrdinal == -1)
            {
                _goalIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalId");
            }

            if (_lookbackWindowOrdinal == -1)
            {
                _lookbackWindowOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "LookbackWindow");
            }

            if (_goalValue == -1)
            {
                _goalValue = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalValue");
            }

            if (_goalTrackingType == -1)
            {
                _goalTrackingType = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalTrackingType");
            }

            if (_accountIdOrdinal == -1)
            {
                _accountIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "AccountId");
            }

            if (_goalValueSourceIdOrdinal == -1)
            {
                _goalValueSourceIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalValueSourceId");
            }

            TagId = (int)row[_tagIdOrdinal];
            GoalId = (int)row[_goalIdOrdinal];
            LookbackWindow = (int)row[_lookbackWindowOrdinal];
            GoalValue = (double?)row[_goalValue];
            GoalTrackingType = (short)row[_goalTrackingType];
            AccountId = (int?)row[_accountIdOrdinal];
            GoalValueSourceId = (short)row[_goalValueSourceIdOrdinal];
        }

        /// <summary>
        /// Checks if a visit meet goal requirements and returns a collection of conversions for the visits.
        /// A conversion represents some sort of result achieved during a site visit.
        /// </summary>
        /// <param name="visit"></param>
        /// <param name="isAchieved"></param>
        /// <returns></returns>
        public abstract List<Conversion> FindConversionsIn(Visit visit, out bool isAchieved);

        public double? FetchGoalValue(double? userInterfaceGoalValue, double? dynamicGoalValue, short goalValueSourceId)
        {
            if (goalValueSourceId == (short)GoalValueSource.UserInterface)
            {
                return userInterfaceGoalValue;
            }

            if (goalValueSourceId == (short)GoalValueSource.DynamicGoalValue)
            {
                return dynamicGoalValue ?? userInterfaceGoalValue;
            }

            if (goalValueSourceId == (short)GoalValueSource.IgnoreGoalValue)
            {
                return 0;
            }

            return 0;
        }

        #region OnlyUsedInTests
        /// <summary>
        /// Collection of properties is used to populate Goal-To-Tag Test map file. 
        /// Order of properties should match to the map file schema.
        /// </summary>
        public virtual object[] Properties
        {
            get
            {
                var properties = new object[7];
                properties[0] = TagId;
                properties[1] = GoalId;
                properties[2] = LookbackWindow;
                properties[3] = ((object)GoalValue ?? DBNull.Value);
                properties[4] = GoalTrackingType;
                properties[5] = AccountId;
                //properties[6] = GoalValueSourceId;
                properties[6] = 2; // 2 is the default value. Want to make sure exisitng tests pass.
                return properties;
            }
        }
        #endregion
    }
}
