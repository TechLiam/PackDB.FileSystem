using System.Collections.Generic;
using System.Linq;
using PackDB.Core;
using PackDB.Core.Data;
using PackDB.Core.Indexing;

namespace PackDB.FileSystem.IndexWorker
{
    public class FileIndexWorker : IFileIndexWorker
    {
        private IFileStreamer FileStreamer { get; set; }
        
        public FileIndexWorker(IFileStreamer fileStreamer)
        {
            FileStreamer = fileStreamer;
        }

        private static string GetFileName<TDataType>(string indexName)
        {
            return $"{typeof(TDataType).Name}\\{indexName}.index";
        }

        public bool IndexExist<TDataType>(string indexName) where TDataType : DataEntity
        {
            return FileStreamer.Exists(GetFileName<TDataType>(indexName));
        }

        public IEnumerable<int> GetIdsFromIndex<TDataType,TKeyType>(string indexName, TKeyType indexKey) where TDataType : DataEntity
        {
            var index = FileStreamer.ReadDataFromStream<Index<TKeyType>>(GetFileName<TDataType>(indexName));
            if (index.Keys == null) return new int[0];
            var key = index.Keys.FirstOrDefault(x => x.Value.Equals(indexKey));
            return key is null ? new int[0] : key.Ids.ToArray();
        }

        public bool Index<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var indexProperties = typeof(TDataType)
                .GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(IndexAttribute), true).Any())
                .ToArray();
            foreach (var indexProperty in indexProperties)
            {
                var indexName = indexProperty.Name;
                Index<object> index;
                var indexKey = indexProperty.GetValue(data);
                var indexFileName = GetFileName<TDataType>(indexName);
                if (IndexExist<TDataType>(indexName))
                {
                    index = FileStreamer.ReadDataFromStream<Index<object>>(indexFileName);
                    var otherKeys = index.Keys
                        .Where(x => !x.Value.Equals(indexKey) && x.Ids.Any(y => y == data.Id))
                        .ToArray();
                    foreach (var otherKey in otherKeys)
                    {
                        otherKey.Ids.Remove(data.Id);
                    }
                    var key = index.Keys.FirstOrDefault(x => x.Value.Equals(indexKey));
                    if (key is null)
                    {
                        index.Keys.Add(new IndexKey<object>
                        {
                            Value = indexKey,
                            Ids = new int[]{data.Id}
                        });
                    }
                    else
                    {
                        if (key.Ids.All(x => x != data.Id))
                        {
                            key.Ids.Add(data.Id);
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    index = new Index<object>()
                    {
                        Keys = new List<IndexKey<object>>()
                        {
                            new IndexKey<object>()
                            {
                                Value = indexKey,
                                Ids = new List<int>()
                                {
                                    data.Id
                                }
                            }
                        }
                    };
                }

                if (FileStreamer.WriteDataToStream(indexFileName, index))
                {
                    return FileStreamer.CloseStream(indexFileName);
                }
            }
            return false;
        }

        public bool Unindex<TDataType>(TDataType data) where TDataType : DataEntity
        {
            
            throw new System.NotImplementedException();
        }

    }
}