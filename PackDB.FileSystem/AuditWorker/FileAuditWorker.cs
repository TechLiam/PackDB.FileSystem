using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using PackDB.Core.Auditing;
using PackDB.Core.Data;

namespace PackDB.FileSystem.AuditWorker
{
    public class FileAuditWorker : IFileAuditWorker
    {
        [ExcludeFromCodeCoverage]
        public FileAuditWorker() : this(new FileStreamer())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IFileStreamer fileStreamer) : this(fileStreamer,new AuditGenerator())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IAuditGenerator auditGenerator) : this(new FileStreamer(),auditGenerator)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(string dataFolder) : this(new FileStreamer(),new AuditGenerator(), dataFolder)
        {
        }

        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IFileStreamer fileStreamer, string dataFolder) : this(fileStreamer, new AuditGenerator(), dataFolder)
        {
        }
        
        [ExcludeFromCodeCoverage]
        public FileAuditWorker(IAuditGenerator auditGenerator, string dataFolder) : this(new FileStreamer(), auditGenerator, dataFolder)
        {
        }

        public FileAuditWorker(IFileStreamer fileStreamer, IAuditGenerator auditGenerator, string dataFolder = FileSystemConstants.DataFolder)
        {
            FileStreamer = fileStreamer;
            AuditGenerator = auditGenerator;
            TopLevelDataFolderName = dataFolder;
        }

        private IFileStreamer FileStreamer { get; }
        private IAuditGenerator AuditGenerator { get; }
        private string TopLevelDataFolderName { get; }

        public Task<bool> CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.NewLog(data));
        }

        public async Task<bool> UpdateEvent<TDataType>(TDataType newData, TDataType oldData) where TDataType : DataEntity
        {
            var currentLog = await ReadAllEvents<TDataType>(newData.Id);
            return await WriteEvent(GetFileName<TDataType>(newData.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.UpdateLog(newData, oldData, currentLog));
        }

        public async Task<bool> DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentLog = await ReadAllEvents<TDataType>(data.Id);
            return await WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.DeleteLog(data, currentLog));
        }

        public async Task<bool> UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentLog = await ReadAllEvents<TDataType>(data.Id);
            return await WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.UndeleteLog(data, currentLog));
        }

        public async Task<bool> RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentLog = await ReadAllEvents<TDataType>(data.Id);
            return await WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.RollbackLog(data, currentLog));
        }

        public async Task<bool> CommitEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(data.Id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    if (await FileStreamer.CloseStream(filename))
                    {
                        await FileStreamer.UnlockFile(filename);
                        return true;
                    }
                }
                catch
                {
                }
            }

            await DiscardEvents(data);
            return false;
        }

        public async Task DiscardEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(data.Id);
            await FileStreamer.DisposeOfStream(filename);
            await FileStreamer.UnlockFile(filename);
        }

        public async Task<AuditLog> ReadAllEvents<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                if (await FileStreamer.GetLockForFile(filename))
                    try
                    {
                        return await FileStreamer.ReadDataFromStream<AuditLog>(filename);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        await FileStreamer.UnlockFile(filename);
                    }
            }

            return null;
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
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                if (await FileStreamer.GetLockForFile(filename))
                    try
                    {
                        if (await FileStreamer.WriteDataToStream(filename, generateLog.Invoke())) return true;
                        await FileStreamer.UnlockFile(filename);
                    }
                    catch
                    {
                        await FileStreamer.UnlockFile(filename);
                    }
            }

            return false;
        }
    }
}