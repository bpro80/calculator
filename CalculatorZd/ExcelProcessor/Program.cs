using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var myData = new List<DataForTest>
            {
                new DataForTest("a1", "b1", "c1", "d1", "e1"),
                new DataForTest("a1", "b1", "c1", "d1", "e1")
            };

            var ex = new ConvertToDataTable();
            //ex.ExcelTableLines(myData) - конвертируем наши данные в DataTable
            //ex.ExcelTableHeader(myData.Count) - формируем данные для Label
            //template - указываем название нашего файла  - шаблона
          //  new Worker().Export(ex.ExcelTableLines(myData), ex.ExcelTableHeader(myData.Count), "template");

        }
    }
}
