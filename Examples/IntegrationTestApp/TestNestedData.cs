using System.Diagnostics.CodeAnalysis;
using MessagePack;
using PackDB.Core.Data;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    public class TestNestedData : DataEntity
    {
        [Key(2)]
        public string Value { get; set; }
    }
}