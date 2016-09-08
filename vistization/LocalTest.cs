using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AdCenter.BI.UET.StreamingSchema;

namespace VisitizationStreaming
{
    class LocalTest
    {
        public void test()
        {
            FileStream fs = new FileStream(@"D:\VistizationDataFile\uetlog.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            string data = sr.ReadLine();
            var uet = UETLogView.Deserialize(data);
        }
    }
}
