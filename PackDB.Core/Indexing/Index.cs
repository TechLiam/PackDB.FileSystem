using System.Collections.Generic;

namespace PackDB.Core.Indexing
{
    public class Index<TKeyType>
    {
        public ICollection<IndexKey<TKeyType>> Keys { get; set; }
    }
}