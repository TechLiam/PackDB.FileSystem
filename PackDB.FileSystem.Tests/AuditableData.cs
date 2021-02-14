using System.Diagnostics.CodeAnalysis;
using PackDB.Core.Auditing;

namespace PackDB.FileSystem.Tests
{
    [Audit(MaxAttempts = 3)]
    [ExcludeFromCodeCoverage]
    public class AuditableData : BasicData
    {
        public string Value1 { get; set; }
        public int Value2 { get; set; }
    }
}