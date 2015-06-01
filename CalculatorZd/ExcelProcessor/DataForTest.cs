using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelProcessor
{
    public class DataForTest : IDataForTest
    {
        public String A { get; private set; }
        public String B { get; private set; }
        public String C { get; private set; }
        public string D { get; private set; }
        public string E { get; private set; }
        public string F { get; private set; }

        public DataForTest(String a, String b, String c, string d, string e)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
        }

        public DataForTest(DataRow item)
        {
            A = item[0].ToString();
            B = item[1].ToString();
            C = item[2].ToString();
        }
    }
}
