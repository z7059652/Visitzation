using Microsoft.AdCenter.BI.UET.Schema;
using System.Text.RegularExpressions;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    public class GoalProperty<T>
    {
        public T PropertyValue;
        public GoalComparisonOperator ComparisonOperator;
        public ValueComparisonOperator ValueComparisionOperator;
        public bool RegexAssigned;
        public Regex Regex;
    }
}
