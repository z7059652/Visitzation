using Microsoft.AdCenter.BI.UET.Schema;
using ScopeRuntime.Diagnostics;
using System;
using System.Text.RegularExpressions;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    public static class GoalUtils
    {
        private static readonly TimeSpan RegexTimeOut = TimeSpan.FromMilliseconds(100);

        // Checks if operator value is in valid range
        public static bool IsValidValueOperator(byte comparisonOperator)
        {
            return (comparisonOperator <= (byte)ValueComparisonOperator.GreaterThan);
        }

        public static bool IsValidComparisonOperator(byte comparisonOperator)
        {
            return (comparisonOperator <= 7);   // 7 accounts for contains
        }

        // Checks if an operator is supported for string values
        // TODO - New enum value 'Contains' should be added to GoalcomparisonOperator.bond. 
        public static bool IsValidStringOperator(GoalComparisonOperator comparisonOperator)
        {
            return comparisonOperator == GoalComparisonOperator.BeginsWith ||
                   comparisonOperator == GoalComparisonOperator.EqualsTo ||
                   comparisonOperator == GoalComparisonOperator.RegularExpression || (int)comparisonOperator == 7;  // 7 denotes contains operator.
        }

        // Checks if an operator is supported for numeric values
        public static bool IsValidNumericOperator(ValueComparisonOperator valueOperator)
        {
            return valueOperator == ValueComparisonOperator.GreaterThan ||
                   valueOperator == ValueComparisonOperator.EqualsTo ||
                   valueOperator == ValueComparisonOperator.LessThan;
        }

        // Compares two strings using the specified operator
        public static bool CompareStringProperty(GoalProperty<string> prop, string value, bool compareUrls = false)
        {
            if (prop == null || String.IsNullOrWhiteSpace(prop.PropertyValue) || String.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var propertyValue = prop.PropertyValue;
            value = value.Trim();

            if ((int)prop.ComparisonOperator == 7)
            {
                // Hard coded & int casted because we will soon be dropping Conman + rewriting/merging the Operator enums
                // 7: Operator value denoting "Contains" eg does the destination url contain the given property value
                return value.IndexOf(propertyValue, StringComparison.OrdinalIgnoreCase) != -1;
            }

            if (prop.ComparisonOperator == GoalComparisonOperator.RegularExpression)
            {
                if (!prop.RegexAssigned)
                {
                    prop.RegexAssigned = true;

                    var strippedPropertyValue = propertyValue.TrimEnd('\\', '/');

                    if (strippedPropertyValue.StartsWith("*"))
                    {
                        // One common mistake in the customer defined url regular expression is forgetting to add the '.' before '*'.
                        // So ".*thankyou.html" is correct but "*thankyou.html" will throw exception during new Regex operation.
                        strippedPropertyValue = "." + strippedPropertyValue;
                    }
                    else if (strippedPropertyValue.StartsWith("?"))
                    {
                        // One common mistake in the customer defined url regular expression is forgetting to add the '\' before '?'.
                        // So "\?thankyou.html" is correct but "?thankyou.html" will throw exception during new Regex operation.
                        strippedPropertyValue = @"\" + strippedPropertyValue;
                    }

                    if (strippedPropertyValue.Contains(@"\_"))
                    {
                        // Another common mistake in the customer defined url regular expression is that they use '\_' to represent '_',
                        // while in C# _ is not a escape character.
                        strippedPropertyValue = strippedPropertyValue.Replace(@"\_", @"_");
                        strippedPropertyValue = strippedPropertyValue.Replace(@"\_", @"\\_"); // We are seeing an "\\_" case and should not replace this one.
                    }

                    try
                    {
                        // Original code first try non-trimmed first then trimmed, which is unnecessary. We can try directly the trimmed.
                        prop.Regex = new Regex(strippedPropertyValue, RegexOptions.IgnoreCase, RegexTimeOut);
                    }
                    catch (ArgumentException)
                    {
                        // TODO: UI needs to validate the regex
                        // DebugStream.WriteLine("Failed to parse regex " + propertyValue);
                        return false;
                    }
                }

                if (prop.Regex != null)
                {
                    try
                    {
                        return prop.Regex.IsMatch(value);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        DebugStream.WriteLine("Timeout occured for " + propertyValue + " when matching " + value);
                    }
                }

                return false;
            }

            var result = false;
            switch (prop.ComparisonOperator)
            {
                case GoalComparisonOperator.EqualsTo:
                    result = String.Equals(value, propertyValue, StringComparison.OrdinalIgnoreCase);
                    break;
                case GoalComparisonOperator.BeginsWith:
                    result = value.StartsWith(propertyValue, StringComparison.OrdinalIgnoreCase);
                    break;
            }

            if (!result && compareUrls)
            {
                // If the original strings do not match, try matching pure URLs (without http or www)

                var strippedPropertyValue = StripHeaders(propertyValue.TrimEnd('/'));
                var strippedValue = StripHeaders(value.TrimEnd('/'));

                switch (prop.ComparisonOperator)
                {
                    case GoalComparisonOperator.EqualsTo:
                        result = String.Equals(strippedValue, strippedPropertyValue, StringComparison.OrdinalIgnoreCase);
                        break;
                    case GoalComparisonOperator.BeginsWith:
                        result = strippedValue.StartsWith(strippedPropertyValue, StringComparison.OrdinalIgnoreCase);
                        break;
                }
            }

            return result;
        }

        // Remove the protocol and www from the beginning of a URL.
        // It will return abc.com for all of the following URLs:
        //  https://www.abc.com
        //  http://www.abc.com
        //  http://abc.com
        //  www.abc.com
        //  abc.com
        private static string StripHeaders(string urlString)
        {
            if (urlString.StartsWith("http://"))
            {
                urlString = urlString.Substring(7);
            }
            else if (urlString.StartsWith("https://"))
            {
                urlString = urlString.Substring(8);
            }

            if (urlString.StartsWith("www."))
            {
                urlString = urlString.Substring(4);
            }

            return urlString;
        }

        // Compares two numeric values using the specified operator
        public static bool CompareNumericProperty<T>(GoalProperty<T> prop, T value) where T : struct, IComparable
        {
            var result = false;
            var compareToResult = value.CompareTo(prop.PropertyValue);

            switch (prop.ValueComparisionOperator)
            {
                case ValueComparisonOperator.GreaterThan:
                    result = (compareToResult == 1);
                    break;
                case ValueComparisonOperator.LessThan:
                    result = (compareToResult == -1);
                    break;
                case ValueComparisonOperator.EqualsTo:
                    result = (compareToResult == 0);
                    break;
            }

            return result;
        }
    }
}
