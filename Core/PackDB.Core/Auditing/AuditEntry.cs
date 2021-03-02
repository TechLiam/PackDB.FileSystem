using System.Collections.Generic;
using MessagePack;

namespace PackDB.Core.Auditing
{
    [MessagePackObject]
    public class AuditEntry
    {
        [Key(1)] public AuditType Type { get; set; }

        [Key(2)] public ICollection<AuditProperty> Changes { get; set; }
    }
}