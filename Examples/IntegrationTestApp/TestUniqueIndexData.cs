using System.Diagnostics.CodeAnalysis;
using MessagePack;
using PackDB.Core;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    public class TestUniqueIndexData : TestData
    {
        [Index(IsUnique = true)] [Key(5)] public string PhoneNumber { get; set; }
    }
}