using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO
{
    public class QueryStatistic
    {
        public int ID { get; set; }
        public string FirmName { get; set; }
        public string TextQuery { get; set; }
        public string TextError { get; set; }
        public string IP { get; set; }
        public string QueryStatisticStatusName { get; set; }
        
        public string TimeBegin { get; set; }
        public string TimeEnd { get; set; }
        public DateTime AddDate { get; set; }
        public int QueryStatusID { get; set; }

        
    }
}
