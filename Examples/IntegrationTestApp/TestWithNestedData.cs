using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using PackDB.Core.Data;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    public class TestWithNestedData : DataEntity
    {
        [Key(2)]
        public string Name { get; set; }
        [Key(3)]
        public IEnumerable<TestNestedData> Nested { get; set; }
    }
}