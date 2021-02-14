using System.Collections.Generic;
using MessagePack;

namespace PackDB.Core.Auditing
{
    [MessagePackObject]
    public class AuditLog
    {
        [Key(1)] public ICollection<AuditEntry> Entries { get; set; }
    }
}