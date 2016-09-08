using System;
using System.Collections.Generic;

namespace Microsoft.AdCenter.BI.UET.Visitization.VisitizationStreamingCommon
{
    /// <summary>
    ///   Deduping identical records from the UET log. 
    /// </summary>
    [Serializable]
    public class UETLogDedupReducer 
    {
        private const char delimeter = '\n';
        private UETLogDedupReducer()
        {

        }
        private static UETLogDedupReducer instance = null;
        public static UETLogDedupReducer INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new UETLogDedupReducer();
                return instance;
            }
        }
        public string GetData(KeyValuePair<string,string> line)
        {
            // copy the first row only

            string[] rows = line.Value.Split(new char[] { delimeter });
            foreach(var row in rows)
            {
                if(!string.IsNullOrEmpty(row))
                    return row;
            }
            return rows[0];
        }
       
    }
}
