using MessagePack;
using PackDB.Core.Data;

namespace IntegrationTestApp
{
    public class TestNestedData : DataEntity
    {
        [Key(2)]
        public string Value { get; set; }
    }
}