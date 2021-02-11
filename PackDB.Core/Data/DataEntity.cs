using MessagePack;

namespace PackDB.Core.Data
{
    [MessagePackObject]
    public class DataEntity
    {
        [Key(1)]
        public int Id { get; set; }
    }
}