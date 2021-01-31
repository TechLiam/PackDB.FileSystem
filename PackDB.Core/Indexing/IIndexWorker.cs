using System.Collections.Generic;
using PackDB.Core.Data;

namespace PackDB.Core
{
    public interface IIndexWorker
    {
        bool IndexExist(string indexName);
        IEnumerable<int> GetIdsFromIndex<TKeyType>(string indexName, TKeyType indexKey);
        bool Index<TDataType>(TDataType data) where TDataType : DataEntity;
        bool Unindex<TDataType>(TDataType data) where TDataType : DataEntity;
    }
}