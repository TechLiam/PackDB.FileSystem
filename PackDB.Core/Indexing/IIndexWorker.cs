using System.Collections.Generic;
using PackDB.Core.Data;

namespace PackDB.Core.Indexing
{
    public interface IIndexWorker
    {
        bool IndexExist<TDataType>(string indexName) where TDataType : DataEntity;
        IEnumerable<int> GetIdsFromIndex<TDataType,TKeyType>(string indexName, TKeyType indexKey) where TDataType : DataEntity;
        bool Index<TDataType>(TDataType data) where TDataType : DataEntity;
        bool Unindex<TDataType>(TDataType data) where TDataType : DataEntity;
    }
}