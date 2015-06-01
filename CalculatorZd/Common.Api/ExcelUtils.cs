using System;
using System.Globalization;
using Microsoft.Office.Interop.Excel;

namespace Common.Api
{
    public class ExcelUtils
    {
        public static string GetDefaultExtension(Application application)
        {
            double Version = Convert.ToDouble(application.Version, CultureInfo.InvariantCulture);
            if (Version >= 12.00)
                return ".xlsx";
            return ".xls";
        }
    }
}