using System.Collections.Generic;

namespace PackDB.Core.Auditing
{
    public class AuditLog
    {
        public ICollection<AuditEntry> Entries { get; set; }
    }
}