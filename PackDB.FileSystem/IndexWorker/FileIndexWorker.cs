using System.Collections.Generic;
using PackDB.Core.Data;

namespace PackDB.FileSystem.IndexWorker
{
    public class FileIndexWorker : IFileIndexWorker
    {
        public bool IndexExist(string indexName)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<int> GetIdsFromIndex<TKeyType>(string indexName, TKeyType indexKey)
        {
            throw new System.NotImplementedException();
        }

        public bool Index<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public bool Unindex<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }
    }
}