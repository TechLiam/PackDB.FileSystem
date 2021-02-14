using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PackDB.Core;
using PackDB.Core.Data;
using PackDB.Core.Indexing;

namespace PackDB.FileSystem.IndexWorker
{
    public class FileIndexWorker : IFileIndexWorker
    {
        [ExcludeFromCodeCoverage]
        public FileIndexWorker() : this(new FileStreamer())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileIndexWorker(string dataPath) : this(new FileStreamer(),dataPath)
        {
        }

        public FileIndexWorker(IFileStreamer fileStreamer, string dataFolder = FileSystemConstants.DataFolder)
        {
            FileStreamer = fileStreamer;
            TopLevelDataFolderName = dataFolder;
        }

        private IFileStreamer FileStreamer { get; }

        private string TopLevelDataFolderName { get; }

        public bool IndexExist<TDataType>(string indexName) where TDataType : DataEntity
        {
            return FileStreamer.Exists(GetFileName<TDataType>(indexName));
        }

        public IEnumerable<int> GetIdsFromIndex<TDataType, TKeyType>(string indexName, TKeyType indexKey)
            where TDataType : DataEntity
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
            if (!indexProperties.Any()) return true;
            var indexSuccess = true;
            foreach (var indexProperty in indexProperties)
            {
                var indexName = indexProperty.Name;
                Index<object> index;
                var indexKey = indexProperty.GetValue(data);
                var indexFileName = GetFileName<TDataType>(indexName);
                var hasChanges = false;
                if (IndexExist<TDataType>(indexName))
                {
                    index = FileStreamer.ReadDataFromStream<Index<object>>(indexFileName);
                    var otherKeys = index.Keys
                        .Where(x => !x.Value.Equals(indexKey) && x.Ids.Any(y => y == data.Id))
                        .ToArray();
                    if (otherKeys.Any()) hasChanges = true;
                    foreach (var otherKey in otherKeys) otherKey.Ids.Remove(data.Id);
                    var key = index.Keys.FirstOrDefault(x => x.Value.Equals(indexKey));
                    if (key is null)
                    {
                        index.Keys.Add(new IndexKey<object>
                        {
                            Value = indexKey,
                            Ids = new[] {data.Id}
                        });
                        hasChanges = true;
                    }
                    else
                    {
                        if (key.Ids.All(x => x != data.Id))
                        {
                            key.Ids.Add(data.Id);
                            hasChanges = true;
                        }
                    }
                }
                else
                {
                    index = new Index<object>
                    {
                        Keys = new List<IndexKey<object>>
                        {
                            new IndexKey<object>
                            {
                                Value = indexKey,
                                Ids = new List<int>
                                {
                                    data.Id
                                }
                            }
                        }
                    };
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    if (FileStreamer.WriteDataToStream(indexFileName, index))
                    {
                        if (!FileStreamer.CloseStream(indexFileName)) indexSuccess = false;
                    }
                    else
                    {
                        indexSuccess = false;
                    }
                }
            }

            return indexSuccess;
        }

        public bool Unindex<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var indexProperties = typeof(TDataType)
                .GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(IndexAttribute), true).Any())
                .ToArray();
            if (!indexProperties.Any()) return true;
            var unindexSuccess = true;
            foreach (var indexProperty in indexProperties)
            {
                var indexName = indexProperty.Name;
                if (IndexExist<TDataType>(indexName))
                {
                    var indexFileName = GetFileName<TDataType>(indexName);
                    var index = FileStreamer.ReadDataFromStream<Index<object>>(indexFileName);
                    if (index.Keys is null) continue;
                    var keys = index.Keys
                        .Where(x => x.Ids.Any(y => y == data.Id))
                        .ToArray();
                    if (!keys.Any()) continue;
                    foreach (var key in keys)
                    {
                        key.Ids.Remove(data.Id);
                        if (!key.Ids.Any()) index.Keys.Remove(key);
                    }

                    if (index.Keys.Any())
                    {
                        if (FileStreamer.WriteDataToStream(indexFileName, index))
                        {
                            if (!FileStreamer.CloseStream(indexFileName)) unindexSuccess = false;
                        }
                        else
                        {
                            unindexSuccess = false;
                        }
                    }
                    else
                    {
                        if (!FileStreamer.Delete(indexFileName)) unindexSuccess = false;
                    }
                }
            }

            return unindexSuccess;
        }

        private string GetFileName<TDataType>(string indexName)
        {
            return $"{TopLevelDataFolderName}\\{typeof(TDataType).Name}\\{indexName}.index";
        }
    }
}