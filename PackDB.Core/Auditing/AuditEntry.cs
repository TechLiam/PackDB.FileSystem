using System.Collections.Generic;

namespace PackDB.Core.Auditing
{
    public class AuditEntry
    {
        
        public AuditType Type { get; set; }
        public ICollection<AuditProperty> Changes { get; set; }
        
    }
}