using System.Collections.Generic;
using MessagePack;
using PackDB.Core.Data;

namespace IntegrationTestApp
{
    public class TestWithNestedData : DataEntity
    {
        [Key(2)]
        public string Name { get; set; }
        [Key(3)]
        public IEnumerable<TestNestedData> Nested { get; set; }
    }
}