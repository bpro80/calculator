using System.Collections.Generic;
using BO;

namespace CalculatorZd.Models
{
    public class AdminViewModel
    {
        public AdminViewModel()
        {
            FirmQueryStatistics = new List<QueryStatistic>();
        }

        public List<QueryStatistic> FirmQueryStatistics { get; set; }
    }
}