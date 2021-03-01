using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackDB.Core.Data;
using PackDB.FileSystem.Attributes;

namespace PackDB.FileSystem.DataWorker
{
    public class FileDataWorker : IFileDataWorker
    {
        [ExcludeFromCodeCoverage]
        public FileDataWorker(ILogger logger) : this(new FileStreamer(logger), logger)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileDataWorker(string dataFolder, ILogger logger) : this(new FileStreamer(logger), logger, dataFolder)
        {
        }

        public FileDataWorker(IFileStreamer fileStreamer, ILogger logger, string dataFolder = FileSystemConstants.DataFolder)
        {
            using (logger.BeginScope("{Operation}", nameof(FileDataWorker)))
            {
                FileStreamer = fileStreamer;
                _logger = logger;
                TopLevelDataFolderName = dataFolder;
                _logger.LogInformation("Created data worker");
            }
        }

        private IFileStreamer FileStreamer { get; }
        private readonly ILogger _logger;
        
        private string TopLevelDataFolderName { get; }

        public async Task<bool> Write<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "writing", typeof(TDataType).Name, id))
            {
                var filename = GetFileName<TDataType>(id);
                var maxAttempts = MaxAttempts<TDataType>();
                _logger.LogTrace("Will try to write to {filename} {MaxAttempts} times", filename, maxAttempts);
                var attempts = 0;
                while (attempts < maxAttempts)
                {
                    attempts++;
                    _logger.LogTrace("Attempt number {Attempt}", attempts);
                    if (await FileStreamer.GetLockForFile(filename))
                        try
                        {
                            _logger.LogTrace("Got a lock on the file");
                            if (await FileStreamer.WriteDataToStream(filename, data))
                            {
                                _logger.LogInformation("Wrote the data to the file system");
                                return true;
                            }
                            _logger.LogWarning("Failed to write data to the file system");
                            await FileStreamer.UnlockFile(filename);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogWarning(exception,"While writing data to the file system an error happened");
                            await FileStreamer.UnlockFile(filename);
                        }
                }
                _logger.LogError("Failed to write data to the file system");
                return false;
            }
        }

        public async Task<bool> Commit<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "commiting data", typeof(TDataType).Name, id))
            {
                var filename = GetFileName<TDataType>(id);
                var maxAttempts = MaxAttempts<TDataType>();
                _logger.LogTrace("Will try to write to {filename} {MaxAttempts} times", filename, maxAttempts);
                var attempts = 0;
                while (attempts < maxAttempts)
                {
                    attempts++;
                    _logger.LogTrace("Attempt number {Attempt}", attempts);
                    try
                    {
                        if (await FileStreamer.CloseStream(filename))
                        {
                            _logger.LogInformation("Closed the file and committed the data to disk");
                            await FileStreamer.UnlockFile(filename);
                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception,"Failed to commit data to disk as an error happened");
                    }
                }
                _logger.LogError("Failed to commit data to disk");
                await DiscardChanges<TDataType>(id);
                return false;
            }
        }

        public async Task DiscardChanges<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "discarding changes", typeof(TDataType).Name, id))
            {
                var filename = GetFileName<TDataType>(id);
                _logger.LogTrace("Discarding changes to file {filename}",filename);
                await FileStreamer.DisposeOfStream(filename);
                await FileStreamer.UnlockFile(filename);
                _logger.LogInformation("Discarded changes to file");
            }
        }

        public async Task<bool> WriteAndCommit<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "writing and commiting", typeof(TDataType).Name, id))
            {
                return await Write(id, data) && await Commit<TDataType>(id);
            }
        }

        public async Task<TDataType> Read<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "reading", typeof(TDataType).Name, id))
            {
                var filename = GetFileName<TDataType>(id);
                var maxAttempts = MaxAttempts<TDataType>();
                _logger.LogTrace("Will try to read from {filename} {MaxAttempts} times", filename, maxAttempts);
                var attempts = 0;
                while (attempts < maxAttempts)
                {
                    attempts++;
                    _logger.LogTrace("Attempt number {Attempt}", attempts);
                    if (await FileStreamer.GetLockForFile(filename))
                        try
                        {
                            _logger.LogTrace("Got lock on file");
                            return await FileStreamer.ReadDataFromStream<TDataType>(filename);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogWarning(exception,"Failed to read data as an error happened");
                        }
                        finally
                        {
                            _logger.LogTrace("Closing file");
                            await FileStreamer.CloseStream(filename);
                            _logger.LogTrace("Unlocking file");
                            await FileStreamer.UnlockFile(filename);
                        }
                }
                _logger.LogError("Failed to read data");
                return null;
            }
        }

        public Task<bool> Exists<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "checking existence of", typeof(TDataType).Name, id))
            {
                return FileStreamer.Exists(GetFileName<TDataType>(id));
            }
        }

        public Task<bool> Delete<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "deleting", typeof(TDataType).Name, id))
            {
                return IsSoftDelete<TDataType>()
                    ? FileStreamer.SoftDelete(GetFileName<TDataType>(id))
                    : FileStreamer.Delete(GetFileName<TDataType>(id));
            }
        }

        public Task<bool> Undelete<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "undeleting", typeof(TDataType).Name, id))
            {
                return IsSoftDelete<TDataType>()
                    ? FileStreamer.Undelete(GetFileName<TDataType>(id))
                    : Task.FromResult(false);
            }
        }

        public async Task Rollback<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(FileDataWorker), "rollingback", typeof(TDataType).Name, id))
            {
                if (IsSoftDelete<TDataType>())
                {
                    _logger.LogTrace("Data would have been soft deleted so undeleting the data");
                    while (!await FileStreamer.Undelete(GetFileName<TDataType>(id)))
                    {
                        _logger.LogWarning("Failed to restore data when it should be able to");
                    }
                    _logger.LogInformation("Data rolledback");
                    return;
                }
                _logger.LogTrace("Data will need to be recreated");
                while (!await WriteAndCommit(id, data))
                {
                    _logger.LogWarning("Failed to recreate data when it should be able to");
                }
                _logger.LogInformation("Data rolledback");
            }
        }

        public int NextId<TDataType>() where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} for {DataType}", nameof(FileDataWorker), "getting next id", typeof(TDataType).Name))
            {
                var files = FileStreamer.GetAllFileNames(GetFolderName<TDataType>(), "data");
                if (!files.Any())
                {
                    _logger.LogInformation("There are no existing files");
                }
                return files.Any() ? Math.Max(files.Max(int.Parse) + 1, 1) : 1;
            }
        }

        private string GetFolderName<TDataType>()
        {
            return $"{TopLevelDataFolderName}\\{typeof(TDataType).Name}";
        }

        private string GetFileName<TDataType>(int id)
        {
            return $"{GetFolderName<TDataType>()}\\{id}.data";
        }

        private static int MaxAttempts<TDataType>()
        {
            var attributes = typeof(TDataType).GetCustomAttributes(typeof(RetryAttemptsAttribute), true)
                .Cast<RetryAttemptsAttribute>().ToArray();
            if (attributes.Any()) return attributes.Max(x => x.MaxAttempts);
            return 1;
        }

        private static bool IsSoftDelete<TDataType>()
        {
            return typeof(TDataType).GetCustomAttributes(typeof(SoftDeleteAttribute), true).Any();
        }
    }
}