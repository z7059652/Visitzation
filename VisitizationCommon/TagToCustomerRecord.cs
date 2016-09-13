using System;
using System.Collections.Generic;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    /// <summary>
    /// A row in Tag-to-Customer/Account Map file
    /// Tag can be associated to a customer or to an account. 
    /// If Tag is associated to a customer, AdvertiserAccountId is null
    /// </summary>
    [Serializable]
    public class TagToCustomerRecord
    {
        private static int _tagIdOrdinal = -1;
        private static int _customerIdOrdinal = -1;
        private static int _tagNameOrdinal = -1;

        public int TagId { get; set; }
        public int CustomerId { get; set; }
        public string TagName { get; set; }

        public TagToCustomerRecord() { }

        public TagToCustomerRecord(IReadOnlyDictionary<string, int> columnMetadata, IList<object> row)
        {
            if (_tagIdOrdinal == -1)
                _tagIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "TagId");

            if (_customerIdOrdinal == -1)
                _customerIdOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "CustomerId");

            if (_tagNameOrdinal == -1)
                _tagNameOrdinal = MapFileUtils.GetColumnOrdinal(columnMetadata, "TagName");

            TagId = (int)row[_tagIdOrdinal];
            CustomerId = (int)row[_customerIdOrdinal];
            TagName = (string)row[_tagNameOrdinal];
        }
    }
}
