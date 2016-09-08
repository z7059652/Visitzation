using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    // An object to contain the fields within the QueryString
    [Serializable]
    public class EnumeratedQueryString
    {
        public string Version;
        public string PageTitle;
        public int TagId;
        public string TagName;
        public int? AdvertiserId;
        public string NavigatedFromURL;
        public double? GoalValue;
        public sbyte? PageLoad;
        public string EventCategory;
        public string EventAction;
        public string EventLabel;
        public double? EventValue;
        public string EventType;
        public Guid? UETMatchingGuid;
        public string ReferrerURL;
        public bool iframe;
        public string rn = string.Empty;

        // The UET rows with ClickId in their querystring are considered as AppInstall event.
        public string AppInstallClickId;

        // Key-value pairs from Querystring
        private Dictionary<string, string> QSDic = new Dictionary<string, string>();

        // Query String parameter names
        public const string QsParameterVersion = "Ver";
        public const string QsParameterPageTitle = "tl";
        public const string QsParameterNavigatedFromUrl = "r";
        public const string QSParameterReferrerURL = "p";
        public const string QsParameterEventCategory = "ec";
        public const string QsParameterEventAction = "ea";
        public const string QsParameterEventLabel = "el";
        public const string QsParameterEventValue = "ev";
        public const string QsParameterEventType = "evt";
        public const string QsParameterTagId = "ti";
        public const string QsParameterTagName = "Tag";
        public const string QsParameterPageLoad = "lt";
        public const string QsParameterGoalValue = "gv";
        public const string QSParameterUETMatchingMUID = "mid";
        public const string QSParameterAdvertiserId_begin = "/action/";
        public const string QSParameterAdvertiserId_end = "?";
        public const string QSParameterIframe = "ifm";
        public const string QSParameterBCLID = "bclid";
        public const string QSParameterAppInstallPS = "action/aips?";
        public const string QSParameterRandomNumber = "rn";

        public EnumeratedQueryString()
        {
        }

        public bool TryParse(string queryString, char keyValueSeparator = '=', char groupSeparator = '&')
        {
            if (String.IsNullOrWhiteSpace(queryString) || queryString.IndexOf(keyValueSeparator) == -1)
            {
                return false;
            }

            // Remove the /action/[AdvertiserId]? part from QueryString
            var ei = queryString.IndexOf(QSParameterAdvertiserId_end);
            if (!ExtractKeyValuePairs(queryString.Substring(ei + 1), keyValueSeparator, groupSeparator))
            {
                return false;
            }

            // Extract AdvertiserId to be used for Version 1 logs to lookup (TagName, AdvertiserId) -> TagId map file.
            var bi = queryString.IndexOf(QSParameterAdvertiserId_begin);
            if (bi != -1 && ei > bi)
            {
                bi += QSParameterAdvertiserId_begin.Length;
                var advertiserIdStr = queryString.Substring(bi, ei - bi);
                int advertiserIdInt;
                AdvertiserId = int.TryParse(advertiserIdStr, out advertiserIdInt) ? advertiserIdInt : (int?)null;
            }

            // Enumerate string columns
            Version = LookupKeyString(QsParameterVersion);
            PageTitle = LookupKeyString(QsParameterPageTitle);

            // QueryString for AppInstall events start with /action/aips?
            if (queryString.IndexOf(QSParameterAppInstallPS) != -1)
            {
                AppInstallClickId = LookupKeyString(QSParameterBCLID);
            }

            // decode NavigatedFromURL and ReferrerURL
            NavigatedFromURL = WebUtility.UrlDecode(LookupKeyString(QsParameterNavigatedFromUrl));
            ReferrerURL = WebUtility.UrlDecode(LookupKeyString(QSParameterReferrerURL));
            EventCategory = WebUtility.UrlDecode(LookupKeyString(QsParameterEventCategory));
            EventAction = WebUtility.UrlDecode(LookupKeyString(QsParameterEventAction));
            EventLabel = WebUtility.UrlDecode(LookupKeyString(QsParameterEventLabel));
            EventType = LookupKeyString(QsParameterEventType);

            // If the event type is not specified, but the key "ec" exist in the query string, then set type to custom event.
            if (String.IsNullOrEmpty(EventType) && !String.IsNullOrWhiteSpace(EventCategory))
            {
                EventType = "custom";
            }

            TagName = WebUtility.UrlDecode(LookupKeyString(QsParameterTagName));

            UETMatchingGuid = CommonUtils.ParseGuid(LookupKeyString(QSParameterUETMatchingMUID));
            rn = LookupKeyString(QSParameterRandomNumber);

            // Enumerate nullable number columns
            EventValue = LookupKeyDouble(QsParameterEventValue);
            TagId = LookupKeyInt(QsParameterTagId);
            PageLoad = LookupKeySByte(QsParameterPageLoad);
            GoalValue = LookupKeyDouble(QsParameterGoalValue);
            iframe = LookupKeyString(QSParameterIframe) == "1";

            return true;
        }

        private bool ExtractKeyValuePairs(string queryString, char keyValueSeparator, char groupSeparator)
        {
            var QS = queryString.Split(groupSeparator);
            var potentialPairs = QS.Select(p => p.Split(keyValueSeparator)).Where(v => v.Length > 1);
            foreach (var pair in potentialPairs)
            {
                if (QSDic.ContainsKey(pair[0]))
                {
                    // If the values of the duplicate keys match, ignore the dup entry and keep the row
                    string firstValue;
                    QSDic.TryGetValue(pair[0], out firstValue);
                    if (!firstValue.Equals(pair[1]))
                    {
                        return false;
                    }
                }
                else
                {
                    QSDic.Add(pair[0], pair[1]);
                }
            }

            return true;
        }

        private string LookupKeyString(string key)
        {
            string value;
            QSDic.TryGetValue(key, out value);
            return value;
        }

        private double? LookupKeyDouble(string key)
        {
            var stringValue = LookupKeyString(key);
            double value;
            if (double.TryParse(stringValue, out value) && !double.IsNaN(value))
            {
                if (value < 0 || value > 9999999)
                {
                    return 0;
                }

                return value;
            }

            return null;
        }

        private int LookupKeyInt(string key)
        {
            var stringValue = LookupKeyString(key);
            int value;
            return int.TryParse(stringValue, out value) ? value : -1;
        }

        private sbyte? LookupKeySByte(string key)
        {
            var stringValue = LookupKeyString(key);
            sbyte value;
            return sbyte.TryParse(stringValue, out value) ? value : (sbyte?)null;
        }
    }
}
