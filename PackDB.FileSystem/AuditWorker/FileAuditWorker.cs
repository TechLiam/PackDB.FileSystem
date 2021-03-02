using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackDB.Core;
using PackDB.Core.Auditing;
using PackDB.Core.Data;

namespace PackDB.FileSystem.AuditWorker
{
    public class FileAuditWorker : IFileAuditWorker
    {

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(ILogger logger) : this(new FileStreamer(logger), logger)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IFileStreamer fileStreamer) : this(fileStreamer,new EmptyLogger())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IFileStreamer fileStreamer, ILogger logger) : this(fileStreamer, new AuditGenerator(logger), logger)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IAuditGenerator auditGenerator, ILogger logger) : this(new FileStreamer(logger), auditGenerator, logger)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(string dataFolder, ILogger logger) : this(new FileStreamer(logger), new AuditGenerator(logger), logger, dataFolder)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IFileStreamer fileStreamer, string dataFolder, ILogger logger) : this(fileStreamer, new AuditGenerator(logger), logger, dataFolder)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IAuditGenerator auditGenerator, string dataFolder, ILogger logger) : this(new FileStreamer(logger),
            auditGenerator, logger, dataFolder)
        {
        }

        public FileAuditWorker(IFileStreamer fileStreamer, IAuditGenerator auditGenerator) : this(fileStreamer,auditGenerator,new EmptyLogger())
        {
        }
        
        public FileAuditWorker(IFileStreamer fileStreamer, IAuditGenerator auditGenerator, ILogger logger, string dataFolder = FileSystemConstants.DataFolder)
        {
            using (logger.BeginScope("{Operation}", nameof(FileAuditWorker)))
            {
                FileStreamer = fileStreamer;
                AuditGenerator = auditGenerator;
                _logger = logger;
                TopLevelDataFolderName = dataFolder;
                _logger.LogInformation("Created audit worker");
            }
        }

        private ILogger _logger;
        private IFileStreamer FileStreamer { get; }
        private IAuditGenerator AuditGenerator { get; }
        private string TopLevelDataFolderName { get; }

        public Task<bool> CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is logging a {Action} for {DataType}", nameof(FileAuditWorker), "create event", typeof(TDataType).Name))
            {
                return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                    () => AuditGenerator.NewLog(data));
            }
        }

        public async Task<bool> UpdateEvent<TDataType>(TDataType newData, TDataType oldData)
            where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is logging an {Action} for {DataType}", nameof(FileAuditWorker),
                "update event", typeof(TDataType).Name))
            {
                var currentLog = await ReadAllEvents<TDataType>(newData.Id);
                return await WriteEvent(GetFileName<TDataType>(newData.Id), MaxAttempts<TDataType>(),
                    () => AuditGenerator.UpdateLog(newData, oldData, currentLog));
            }
        }

        public async Task<bool> DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is logging a {Action} for {DataType}", nameof(FileAuditWorker),
                "delete event", typeof(TDataType).Name))
            {
                var currentLog = await ReadAllEvents<TDataType>(data.Id);
                return await WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                    () => AuditGenerator.DeleteLog(data, currentLog));
            }
        }

        public async Task<bool> UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is logging an {Action} for {DataType}", nameof(FileAuditWorker),
                "undelete event", typeof(TDataType).Name))
            {
                var currentLog = await ReadAllEvents<TDataType>(data.Id);
                return await WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                    () => AuditGenerator.UndeleteLog(data, currentLog));
            }
        }

        public async Task<bool> RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is logging a {Action} for {DataType}", nameof(FileAuditWorker),
                "rollback event", typeof(TDataType).Name))
            {
                var currentLog = await ReadAllEvents<TDataType>(data.Id);
                return await WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                    () => AuditGenerator.RollbackLog(data, currentLog));
            }
        }

        public async Task<bool> CommitEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} for {DataType}", nameof(FileAuditWorker),
                "committing events", typeof(TDataType).Name))
            {
                var filename = GetFileName<TDataType>(data.Id);
                var maxAttempts = MaxAttempts<TDataType>();
                _logger.LogTrace("Will try to write to {filename} {MaxAttempts} times", filename, maxAttempts >= 0 ? maxAttempts.ToString() : "until success");
                var attempts = 0;
                while (maxAttempts == -1 || attempts < maxAttempts)
                {
                    attempts++;
                    _logger.LogTrace("Attempt number {Attempt}", attempts);
                    try
                    {
                        _logger.LogTrace("Closing audit file");
                        if (await FileStreamer.CloseStream(filename))
                        {
                            _logger.LogTrace("Closed audit file");
                            await FileStreamer.UnlockFile(filename);
                            _logger.LogInformation("Committed audit to file");
                            return true;
                        }
                        _logger.LogWarning("Failed to close audit file");
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception,"Failed to commit audit");
                    }
                }
                
                await DiscardEvents(data);
                _logger.LogWarning("Discarded events after max attempts trying to commit audit");
                return false;
            }
        }

        public async Task DiscardEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} for {DataType}", nameof(FileAuditWorker),
                "discarded events", typeof(TDataType).Name))
            {
                var filename = GetFileName<TDataType>(data.Id);
                _logger.LogTrace("Discarding changes for {filename}", filename);
                await FileStreamer.DisposeOfStream(filename);
                await FileStreamer.UnlockFile(filename);
                _logger.LogInformation("Discarded changes for {filename}", filename);
            }
        }

        public async Task<AuditLog> ReadAllEvents<TDataType>(int id) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is {Action} for {DataType} with id ({id})", nameof(FileAuditWorker),
                "reading all events", typeof(TDataType).Name,id))
            {
                var filename = GetFileName<TDataType>(id);
                var maxAttempts = MaxAttempts<TDataType>();
                _logger.LogTrace("Will try to write to {filename} {MaxAttempts} times", filename, maxAttempts >= 0 ? maxAttempts.ToString() : "until success");
                var attempts = 0;
                while (maxAttempts == -1 || attempts < maxAttempts)
                {
                    attempts++;
                    _logger.LogTrace("Attempt number {Attempt}", attempts);
                    if (await FileStreamer.GetLockForFile(filename))
                    {
                        try
                        {
                            var audit = await FileStreamer.ReadDataFromStream<AuditLog>(filename);
                            _logger.LogInformation("Read audit from disk");
                            return audit;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogWarning(exception,"Failed to read audit");
                        }
                        finally
                        {
                            await FileStreamer.UnlockFile(filename);
                            _logger.LogTrace("Unlocked audit file");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to get a lock for the audit file");
                    }
                }

                return null;
            }
        }

        private string GetFileName<TDataType>(int id)
        {
            return $"{TopLevelDataFolderName}\\{typeof(TDataType).Name}\\{id}.audit";
        }

        [ExcludeFromCodeCoverage]
        private static int MaxAttempts<TDataType>()
        {
            var attributes = typeof(TDataType).GetCustomAttributes(typeof(AuditAttribute), true)
                .Cast<AuditAttribute>().ToArray();
            if (attributes.Any(x => x.MaxAttempts > 0)) return attributes.Max(x => x.MaxAttempts);
            return -1;
        }

        private async Task<bool> WriteEvent(string filename, int maxAttempts, Func<AuditLog> generateLog)
        {
            _logger.LogTrace("Will try to write to {filename} {MaxAttempts} times", filename, maxAttempts >= 0 ? maxAttempts.ToString() : "until success");
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                _logger.LogTrace("Attempt number {Attempt}", attempts);
                if (await FileStreamer.GetLockForFile(filename))
                {
                    try
                    {
                        _logger.LogTrace("Got a lock on the audit file");
                        if (await FileStreamer.WriteDataToStream(filename, generateLog.Invoke()))
                        {
                            _logger.LogInformation("Wrote audit log to audit file");
                            return true;
                        }
                        _logger.LogWarning("Failed to write audit log to audit file");
                        await FileStreamer.UnlockFile(filename);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Failed to write audit log to audit file");
                        await FileStreamer.UnlockFile(filename);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get a lock for the audit file");
                }
            }
            _logger.LogWarning("Failed to write event after max attempts");
            return false;
        }
    }
}