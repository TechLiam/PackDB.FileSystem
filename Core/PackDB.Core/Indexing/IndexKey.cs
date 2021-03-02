using System.Collections.Generic;
using MessagePack;

namespace PackDB.Core.Indexing
{
    [MessagePackObject]
    public class IndexKey<TKeyType>
    {
        [Key(1)] public TKeyType Value { get; set; }

        [Key(2)] public ICollection<int> Ids { get; set; }
    }
}