using System.Collections.Generic;

namespace PackDB.Core.Indexing
{
    public class IndexKey<TKeyType>
    {
        public TKeyType Value { get; set; }
        public ICollection<int> Ids { get; set; }
    }
}