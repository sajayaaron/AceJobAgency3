using System;

namespace AceJobAgency3.Data
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string Activity { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}
