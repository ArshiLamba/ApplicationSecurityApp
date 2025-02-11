using System;

namespace ApplicationSecurityApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Activity { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Member User { get; set; }
    }
}
