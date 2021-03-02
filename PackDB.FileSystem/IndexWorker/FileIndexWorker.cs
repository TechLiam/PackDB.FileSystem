using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackDB.Core;
using PackDB.Core.Data;
using PackDB.Core.Indexing;

namespace PackDB.FileSystem.IndexWorker
{
    public class FileIndexWorker : IFileIndexWorker
    {
        private readonly ILogger _logger;

        [ExcludeFromCodeCoverage]
        public FileIndexWorker(ILogger logger) : this(new FileStreamer(logger), logger)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileIndexWorker(string dataPath) : this(dataPath, new EmptyLogger())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileIndexWorker(string dataPath, ILogger logger) : this(new FileStreamer(logger), logger, dataPath)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileIndexWorker(IFileStreamer fileStreamer) : this(fileStreamer, new EmptyLogger())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileIndexWorker(IFileStreamer fileStreamer, string dataFolder = FileSystemConstants.DataFolder) : this(
            fileStreamer, new EmptyLogger(), dataFolder)
        {
        }

        public FileIndexWorker(IFileStreamer fileStreamer, ILogger logger,
            string dataFolder = FileSystemConstants.DataFolder)
        {
            using (logger.BeginScope("{Operation}", nameof(FileIndexWorker)))
            {
                FileStreamer = fileStreamer;
                _logger = logger;
                TopLevelDataFolderName = dataFolder;
                _logger.LogInformation("Created index worker");
            }
        }

        private IFileStreamer FileStreamer { get; }

        private string TopLevelDataFolderName { get; }

        public Task<bool> IndexExist<TDataType>(string indexName) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {IndexName} for {DataType}", nameof(FileIndexWorker),
                "checking existence of", indexName, typeof(TDataType).Name))
            {
                return FileStreamer.Exists(GetFileName<TDataType>(indexName));
            }
        }

        public async IAsyncEnumerable<int> GetIdsFromIndex<TDataType, TKeyType>(string indexName, TKeyType indexKey)
            where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {IndexName} for {DataType} with {key}",
                nameof(FileIndexWorker), "getting ids from", indexName, typeof(TDataType).Name, indexKey))
            {
                var index = await FileStreamer.ReadDataFromStream<Index<TKeyType>>(GetFileName<TDataType>(indexName));
                var key = index.Keys?.FirstOrDefault(x => x.Value.Equals(indexKey));
                if (key == null)
                {
                    _logger.LogWarning("The index doesn't have the key");
                    yield break;
                }

                foreach (var id in key.Ids)
                {
                    _logger.LogInformation("Returning {id}", id);
                    yield return id;
                }
            }
        }

        public async Task<bool> Index<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} for {DataType}", nameof(FileIndexWorker), "indexing",
                typeof(TDataType).Name))
            {
                var indexProperties = typeof(TDataType)
                    .GetProperties()
                    .Where(x => x.GetCustomAttributes(typeof(IndexAttribute), true).Any())
                    .ToArray();
                if (!indexProperties.Any())
                {
                    _logger.LogTrace("There are no properties to index");
                    return true;
                }

                _logger.LogInformation("Starting to index");
                var indexSuccess = true;
                foreach (var indexProperty in indexProperties)
                {
                    var indexName = indexProperty.Name;
                    _logger.LogTrace("Indexing into {IndexName}", indexName);
                    Index<object> index;
                    var indexKey = indexProperty.GetValue(data);
                    var indexFileName = GetFileName<TDataType>(indexName);
                    using (_logger.BeginScope("The index file {filename} will be used", indexFileName))
                    {
                        var hasChanges = false;
                        if (await IndexExist<TDataType>(indexName))
                        {
                            _logger.LogTrace("Index exists");
                            index = await FileStreamer.ReadDataFromStream<Index<object>>(indexFileName);
                            _logger.LogTrace("Loaded index data");
                            var otherKeys = index.Keys
                                .Where(x => !x.Value.Equals(indexKey) && x.Ids.Any(y => y == data.Id))
                                .ToArray();
                            if (otherKeys.Any())
                            {
                                _logger.LogTrace("The index key for the data has changed");
                                hasChanges = true;
                            }

                            foreach (var otherKey in otherKeys)
                            {
                                otherKey.Ids.Remove(data.Id);
                                _logger.LogTrace("Removed old index key");
                            }

                            var key = index.Keys.FirstOrDefault(x => x.Value.Equals(indexKey));
                            if (key is null)
                            {
                                _logger.LogTrace("Index key doesn't already exist");
                                index.Keys.Add(new IndexKey<object>
                                {
                                    Value = indexKey,
                                    Ids = new[] {data.Id}
                                });
                                hasChanges = true;
                            }
                            else
                            {
                                _logger.LogTrace("Index key already exists");
                                if (key.Ids.All(x => x != data.Id))
                                {
                                    _logger.LogTrace("Added id to index key");
                                    key.Ids.Add(data.Id);
                                    hasChanges = true;
                                }
                            }
                        }
                        else
                        {
                            _logger.LogTrace("Creating {IndexName}", indexName);
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
                            _logger.LogTrace("There are changes to the index");
                            if (await FileStreamer.WriteDataToStream(indexFileName, index))
                            {
                                _logger.LogTrace("Wrote changes to index file");
                                if (!await FileStreamer.CloseStream(indexFileName))
                                {
                                    _logger.LogWarning("Failed to commit data to the disk");
                                    indexSuccess = false;
                                }
                                else
                                {
                                    _logger.LogInformation("Saved changes to index");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Failed to write data to the disk");
                                indexSuccess = false;
                            }
                        }
                    }
                }

                if (indexSuccess)
                    _logger.LogInformation("Indexed data successfully");
                else
                    _logger.LogWarning("Failed to index data");
                return indexSuccess;
            }
        }

        public async Task<bool> Unindex<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} for {DataType}", nameof(FileIndexWorker), "unindexing",
                typeof(TDataType).Name))
            {
                var indexProperties = typeof(TDataType)
                    .GetProperties()
                    .Where(x => x.GetCustomAttributes(typeof(IndexAttribute), true).Any())
                    .ToArray();
                if (!indexProperties.Any())
                {
                    _logger.LogTrace("There are no properties are indexed");
                    return true;
                }

                _logger.LogInformation("Starting to unindex");
                var unindexSuccess = true;
                foreach (var indexProperty in indexProperties)
                {
                    var indexName = indexProperty.Name;
                    _logger.LogTrace("Unindexing {IndexName}", indexName);
                    if (await IndexExist<TDataType>(indexName))
                    {
                        _logger.LogTrace("Index exists");
                        var indexFileName = GetFileName<TDataType>(indexName);
                        using (_logger.BeginScope("The index file {filename} will be used", indexFileName))
                        {
                            var index = await FileStreamer.ReadDataFromStream<Index<object>>(indexFileName);
                            _logger.LogTrace("Loaded index data");
                            if (index.Keys is null)
                            {
                                _logger.LogWarning("There are no keys in the index");
                                continue;
                            }

                            var keys = index.Keys
                                .Where(x => x.Ids.Any(y => y == data.Id))
                                .ToArray();
                            if (!keys.Any())
                            {
                                _logger.LogInformation("There where no keys with the data's Id indexes against it.");
                                continue;
                            }

                            foreach (var key in keys)
                            {
                                key.Ids.Remove(data.Id);
                                _logger.LogTrace("Removed id from index key");
                                if (!key.Ids.Any())
                                {
                                    index.Keys.Remove(key);
                                    _logger.LogTrace("No more ids against the key removed the key from the index");
                                }
                            }

                            if (index.Keys.Any())
                            {
                                _logger.LogTrace("The index still has some keys");
                                if (await FileStreamer.WriteDataToStream(indexFileName, index))
                                {
                                    _logger.LogTrace("Successfully wrote index to the index file");
                                    if (!await FileStreamer.CloseStream(indexFileName))
                                    {
                                        _logger.LogWarning("Failed to save the index to disk");
                                        unindexSuccess = false;
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Index updated");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to write data to the file");
                                    unindexSuccess = false;
                                }
                            }
                            else
                            {
                                _logger.LogTrace("No more keys in the index");
                                if (!await FileStreamer.Delete(indexFileName))
                                {
                                    _logger.LogWarning("Failed to remove index file");
                                    unindexSuccess = false;
                                }
                                else
                                {
                                    _logger.LogInformation("Removed index file");
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogTrace("Index doesn't exist");
                    }
                }

                if (unindexSuccess)
                    _logger.LogInformation("Unindex data successfully");
                else
                    _logger.LogWarning("Failed to unindex data");
                return unindexSuccess;
            }
        }

        private string GetFileName<TDataType>(string indexName)
        {
            return $"{TopLevelDataFolderName}\\{typeof(TDataType).Name}\\{indexName}.index";
        }
    }
}