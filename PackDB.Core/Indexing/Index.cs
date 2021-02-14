using System.Collections.Generic;
using MessagePack;

namespace PackDB.Core.Indexing
{
    [MessagePackObject]
    public class Index<TKeyType>
    {
        [Key(1)] public ICollection<IndexKey<TKeyType>> Keys { get; set; }
    }
}