using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AdCenter.BI.UET.Common.Helpers.MapFiles;
using Microsoft.AdCenter.BI.UET.Schema;

namespace Microsoft.AdCenter.BI.UET.Common.Helpers.GoalMgmtMapRecords
{
    /// <summary>
    /// A row in CustomEventGoalType Tag-to-Goal Map file
    /// </summary>
    public class TagToGoalRecordCustomEvent : TagToGoalRecord
    {
        private static int _eventCategoryOrdinal = -1;
        private static int _categoryComparisonOperatorOrdinal = -1;
        private static int _eventNameOrdinal = -1;
        private static int _nameComparisonOperatorOrdinal = -1;
        private static int _eventLabelOrdinal = -1;
        private static int _labelComparisonOperatorOrdinal = -1;
        private static int _eventValueOrdinal = -1;
        private static int _valueComparisonOperatorOrdinal = -1;

        public GoalProperty<string> EventCategoryProperty;
        public GoalProperty<string> EventActionProperty;
        public GoalProperty<string> EventLabelProperty;
        public GoalProperty<double> EventValueProperty;

        public TagToGoalRecordCustomEvent() { GoalType = GoalType.CustomEvent; }

        public TagToGoalRecordCustomEvent(Dictionary<string, int> columnMetadata, object[] row)
            : base(columnMetadata, row)
        {
            GoalType = GoalType.CustomEvent;

            if (_eventCategoryOrdinal == -1)
            {
                _eventCategoryOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventCategory");
            }

            if (_categoryComparisonOperatorOrdinal == -1)
            {
                _categoryComparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventCategoryOperator");
            }

            if (_eventNameOrdinal == -1)
            {
                _eventNameOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventName");
            }

            if (_nameComparisonOperatorOrdinal == -1)
            {
                _nameComparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventNameOperator");
            }

            if (_eventLabelOrdinal == -1)
            {
                _eventLabelOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventLabel");
            }

            if (_labelComparisonOperatorOrdinal == -1)
            {
                _labelComparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventLabelOperator");
            }

            if (_eventValueOrdinal == -1)
            {
                _eventValueOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventValue");
            }

            if (_valueComparisonOperatorOrdinal == -1)
            {
                _valueComparisonOperatorOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "EventValueOperator");
            }

            var eventCategory = (string)row[_eventCategoryOrdinal];
            var categoryComparisonOperatorByte = (byte?)row[_categoryComparisonOperatorOrdinal];
            EventCategoryProperty = CreateCustomEventGoalProperty("Category", eventCategory, categoryComparisonOperatorByte);

            var eventName = (string)row[_eventNameOrdinal];
            var nameComparisonOperatorByte = (byte?)row[_nameComparisonOperatorOrdinal];
            EventActionProperty = CreateCustomEventGoalProperty("Name", eventName, nameComparisonOperatorByte);

            var eventLabel = (string)row[_eventLabelOrdinal];
            var labelComparisonOperatorByte = (byte?)row[_labelComparisonOperatorOrdinal];
            EventLabelProperty = CreateCustomEventGoalProperty("Label", eventLabel, labelComparisonOperatorByte);

            var eventValue = (double?)row[_eventValueOrdinal];
            var valueComparisonOperatorByte = (byte?)row[_valueComparisonOperatorOrdinal];
            if (eventValue != null)
            {
                if (valueComparisonOperatorByte == null)
                {
                    throw new Exception("ValueComparisonOperator value cannot be null when EventValue value is not null.");
                }

                if (!GoalUtils.IsValidValueOperator((byte)valueComparisonOperatorByte))
                {
                    throw new Exception(String.Format("ValueComparisonOperator value {0} is outside of range.", valueComparisonOperatorByte));
                }

                var valueOperator = (ValueComparisonOperator)valueComparisonOperatorByte;

                if (!GoalUtils.IsValidNumericOperator(valueOperator))
                {
                    throw new Exception(String.Format("ValueOperator value {0} is not supported by {1} goal type.", valueOperator.ToString(), GoalType));
                }

                EventValueProperty = new GoalProperty<double>
                {
                    ValueComparisionOperator = valueOperator,
                    PropertyValue = (double)eventValue
                };
            }
        }

        private GoalProperty<string> CreateCustomEventGoalProperty(string propertyName, string value, byte? comparisonOperatorByte)
        {
            GoalProperty<string> eventCategoryProperty = null;
            var valueColumnName = String.Format("Event{0}", propertyName);
            var operatorColumnName = String.Format("{0}ComparisonOperator", propertyName);

            if (value != null)
            {
                if (comparisonOperatorByte == null)
                {
                    throw new Exception(String.Format("{0} value cannot be null when {1} value is not null.", operatorColumnName, valueColumnName));
                }

                if (!GoalUtils.IsValidComparisonOperator((byte)comparisonOperatorByte))
                {
                    throw new Exception(String.Format("{0} value {1} is outside of range.", operatorColumnName, comparisonOperatorByte));
                }

                var comparisonOperator = (GoalComparisonOperator)comparisonOperatorByte;

                if (!GoalUtils.IsValidStringOperator(comparisonOperator))
                {
                    throw new Exception(String.Format("Comparison operator value {0} is not supported by {1} goal type.", comparisonOperator.ToString(), GoalType));
                }

                eventCategoryProperty = new GoalProperty<string>
                {
                    ComparisonOperator = comparisonOperator,
                    PropertyValue = value.Trim()
                };
            }
            return eventCategoryProperty;
        }

        public override List<Conversion> FindConversionsIn(Visit visit, out bool isAchieved)
        {
            isAchieved = false; // Always false because Custom Event goal can be achieved multiple times per visit
            List<Conversion> conversions = null;

            // At least one property to check against: EventCategoryProperty, EventActionProperty, EventLabelProperty, EventValueProperty is not null
            if (EventCategoryProperty != null || EventActionProperty != null || EventLabelProperty != null || EventValueProperty != null)
            {
                // For each custom event (evnt.customEvent != null)
                // TODO: Drop the EventsIndex32 and EventsIndex properties. They are not used anywhere. And we are putting a List of int for it?
                int evntIndex = -1;

                foreach (var evnt in visit.Events)
                {
                    evntIndex++;

                    if (evnt.customEvent != null)
                    {
                        // For those properties to check against which is also not null, the values in the customEvent must match. If the property to check against is null, then we assume it is a match by default
                        bool isConversion = (EventCategoryProperty == null ? true : GoalUtils.CompareStringProperty(EventCategoryProperty, evnt.customEvent.EventCategory)) &&
                                            (EventActionProperty == null ? true : GoalUtils.CompareStringProperty(EventActionProperty, evnt.customEvent.EventAction)) &&
                                            (EventLabelProperty == null ? true : GoalUtils.CompareStringProperty(EventLabelProperty, evnt.customEvent.EventLabel)) &&
                                            (EventValueProperty == null ? true : (evnt.customEvent.EventValue.HasValue ? GoalUtils.CompareNumericProperty(EventValueProperty, evnt.customEvent.EventValue.Value) : false));

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
                var properties = new object[15];
                base.Properties.CopyTo(properties, 0);
                properties[7] = (EventCategoryProperty != null) ? (object)EventCategoryProperty.PropertyValue : DBNull.Value;
                properties[8] = (EventCategoryProperty != null) ? (object)EventCategoryProperty.ComparisonOperator : DBNull.Value;

                if (EventActionProperty != null)
                {
                    properties[9] = EventActionProperty.PropertyValue;
                    properties[10] = EventActionProperty.ComparisonOperator;
                }
                else
                {
                    properties[9] = DBNull.Value;
                    properties[10] = DBNull.Value;
                }
                if (EventLabelProperty != null)
                {
                    properties[11] = EventLabelProperty.PropertyValue;
                    properties[12] = EventLabelProperty.ComparisonOperator;
                }
                else
                {
                    properties[11] = DBNull.Value;
                    properties[12] = DBNull.Value;
                }
                properties[13] = (EventValueProperty != null) ? (object)EventValueProperty.PropertyValue : DBNull.Value;
                properties[14] = (EventValueProperty != null) ? (object)EventValueProperty.ValueComparisionOperator : DBNull.Value;
                return properties;
            }
        }
        #endregion
    }
}
