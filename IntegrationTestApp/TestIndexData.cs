using System.Diagnostics.CodeAnalysis;
using MessagePack;
using PackDB.Core;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    public class TestIndexData : TestData
    {
        [Index] [Key(5)] public string PhoneNumber { get; set; }
    }
}