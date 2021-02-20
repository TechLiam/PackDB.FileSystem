using System.Collections.Generic;
using System.Threading.Tasks;
using PackDB.Core.Data;

namespace PackDB.Core.Indexing
{
    public interface IIndexWorker
    {
        Task<bool> IndexExist<TDataType>(string indexName) where TDataType : DataEntity;

        IAsyncEnumerable<int> GetIdsFromIndex<TDataType, TKeyType>(string indexName, TKeyType indexKey)
            where TDataType : DataEntity;

        Task<bool> Index<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<bool> Unindex<TDataType>(TDataType data) where TDataType : DataEntity;
    }
}