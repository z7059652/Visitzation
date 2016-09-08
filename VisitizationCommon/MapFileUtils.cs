using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using IDataReader = Microsoft.BI.Common.IO.IDataReader;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    public static class MapFileUtils
    {
        /// <summary>
        /// Get column ordinal by column name
        /// </summary>
        /// <param name="columnMetadata">column definition</param>
        /// <param name="columnName">column name</param>
        /// <returns>column ordinal</returns>
        public static int GetColumnOrdinal(IReadOnlyDictionary<string, int> columnMetadata, string columnName)
        {
            int ordinal;
            if (!columnMetadata.TryGetValue(columnName, out ordinal))
                throw new Exception(String.Format("Expected column {0} is not found", columnName));
            return ordinal;
        }

        /// <summary>
        /// Compares a schema defined in DSV file with actual column definition in the data set
        /// </summary>
        /// <param name="schema">Schema definition from DSV file</param>
        /// <param name="reader">Dataset</param>
        /// <returns>Column metadata (columnName, columnOrdinal)</returns>
        public static Dictionary<string, int> ValidateSchema(DataTable schema, IDataReader reader)
        {
            // check to ensure the schemas match
            if (reader.Schema.Columns.Count < schema.Columns.Count)
            {
                throw new Exception(String.Format(
                   "Schemas must have the same or less number of columns; DSV schema has {0}, while Map File has {1}",
                    schema.Columns.Count, reader.Schema.Columns.Count));
            }

            var columnMetadata = new Dictionary<string, int>();
            for (var i = 0; i < schema.Columns.Count; i++)
            {
                var expected = schema.Columns[i];
                var actual = reader.Schema.Columns[i];

                if (expected.ColumnName != actual.ColumnName)
                    throw new Exception(
                        String.Format("Column name mismatch at index '{0}': expected {1} but got {2}",
                            i.ToString(CultureInfo.InvariantCulture), expected.ColumnName, actual.ColumnName));

                if (expected.DataType != actual.DataType)
                    throw new Exception(
                        String.Format("Data type mismatch in column '{0}': expected {1} but got {2}",
                            expected.ColumnName, expected.DataType, actual.DataType));

                columnMetadata.Add(actual.ColumnName, actual.Ordinal);
            }
            return columnMetadata;
        }
    }
}
