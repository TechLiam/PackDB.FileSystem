using System.Diagnostics.CodeAnalysis;
using PackDB.Core.Auditing;

namespace PackDB.Core.Tests
{
    [Audit]
    [ExcludeFromCodeCoverage]
    public class AuditedEntity : BasicEntity
    {
    }
}