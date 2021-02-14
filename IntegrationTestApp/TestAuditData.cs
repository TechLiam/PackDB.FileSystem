using System.Diagnostics.CodeAnalysis;
using PackDB.Core.Auditing;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    [Audit]
    public class TestAuditData : TestData
    {
    }
}