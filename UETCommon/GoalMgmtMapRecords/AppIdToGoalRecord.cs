namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    public class AppIdToGoalRecord
    {
        public int GoalId { get; set; }
        public string AppPlatform { get; set; }
        public int LookbackWindow { get; set; }
        public double? GoalValue { get; set; }
        public short GoalTrackingType { get; set; }
        public int? AccountId { get; set; }
        public short GoalValueSourceId { get; set; }
    }
}
