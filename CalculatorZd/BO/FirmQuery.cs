using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO
{
    public class FirmQuery
    {
        public int ID { get; set; }
        public string TextQuery { get; set; }
        public DateTime AddDate { get; set; }
        public string FirmName { get; set; }
        public DateTime TimeBegin { get; set; }
        public DateTime TimeEnd { get; set; }
        public string TextError { get; set; }
        public string QueryStatisticStatusName { get; set; }
        public string IP { get; set; }

    }
}
