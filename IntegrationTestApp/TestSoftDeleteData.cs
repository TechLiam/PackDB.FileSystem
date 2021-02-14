using System.Diagnostics.CodeAnalysis;
using PackDB.FileSystem.Attributes;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    [SoftDelete]
    public class TestSoftDeleteData : TestData
    {
    }
}