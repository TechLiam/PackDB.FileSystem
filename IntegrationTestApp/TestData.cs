using MessagePack;
using PackDB.Core.Data;

namespace IntegrationTestApp
{
    public class TestData : DataEntity
    {
        [Key(2)]
        public string Firstname { get; set; }
        [Key(3)]
        public string Lastname { get; set; }
        [Key(4)]
        public int YearOfBirth { get; set; }
    }
}