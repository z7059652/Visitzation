using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using System.Collections.Generic;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// A row in AppInstall Goal map file for a given AppStoreId
    /// </summary>
    public class GoalRecordAppInstall
    {
        private static int _goalIdOrdinal = -1;
        private static int _appStoreIdOrdinal = -1;
        private static int _appPlatformOrdinal = -1;
        private static int _lookbackWindowOrdinal = -1;
        private static int _goalValueOrdinal = -1;
        private static int _AccountIdOrdinal = -1;
        private static int _goalTrackingType = -1;
        private static int _goalValueSourceIdOrdinal = -1;
        private static int _customerIdOrdinal = -1;

        public int GoalId { get; set; }
        public string AppStoreId { get; set; }
        public string AppPlatform { get; set; }
        public int LookbackWindow { get; set; }
        public double? GoalValue { get; set; }
        public int? AccountId { get; set; }
        public short GoalTrackingType { get; set; }
        public short GoalValueSourceId { get; set; }
        public int CustomerId { get; set; }

        public GoalRecordAppInstall() { }

        public GoalRecordAppInstall(IReadOnlyDictionary<string, int> columnMetadata, IList<object> row)
        {
            if (_goalIdOrdinal == -1)
            {
                _goalIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalId");
            }

            if (_appStoreIdOrdinal == -1)
            {
                _appStoreIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "AppStoreId");
            }

            if (_appPlatformOrdinal == -1)
            {
                _appPlatformOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "AppPlatform");
            }

            if (_lookbackWindowOrdinal == -1)
            {
                _lookbackWindowOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "LookbackWindow");
            }

            if (_goalValueOrdinal == -1)
            {
                _goalValueOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalValue");
            }

            if (_AccountIdOrdinal == -1)
            {
                _AccountIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "AccountId");
            }

            if (_goalTrackingType == -1)
            {
                _goalTrackingType = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalTrackingType");
            }

            if (_goalValueSourceIdOrdinal == -1)
            {
                _goalValueSourceIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "GoalValueSourceId");
            }

            if (_customerIdOrdinal == -1)
            {
                _customerIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "CustomerId");
            }

            GoalId = (int)row[_goalIdOrdinal];
            AppStoreId = (string)row[_appStoreIdOrdinal];
            AppPlatform = (string)row[_appPlatformOrdinal];
            LookbackWindow = (int)row[_lookbackWindowOrdinal];
            GoalValue = (double?)row[_goalValueOrdinal];
            AccountId = (int?)row[_AccountIdOrdinal];
            GoalTrackingType = (short)row[_goalTrackingType];
            GoalValueSourceId = (short)row[_goalValueSourceIdOrdinal];
            CustomerId = (int)row[_customerIdOrdinal];
        }
    }
}
