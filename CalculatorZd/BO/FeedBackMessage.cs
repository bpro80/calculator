using System;

namespace BO
{
    public class FeedBackMessage
    {
        public int ID;
        public string Text;
        public DateTime AddDate;
        public bool IsAdmin;
        public int ParentID;
        public Guid FirmID;
        public string FirmName;
    }
}
