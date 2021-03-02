using System;

namespace PackDB.Core.Auditing
{
    public class AuditAttribute : Attribute
    {
        public int MaxAttempts { get; set; }
    }
}