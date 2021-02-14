using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        public bool CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.NewLog(data));
        }

        public bool UpdateEvent<TDataType>(TDataType newData, TDataType oldData) where TDataType : DataEntity
        {
            var currentLog = ReadAllEvents<TDataType>(newData.Id);
            return WriteEvent(GetFileName<TDataType>(newData.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.UpdateLog(newData, oldData, currentLog));
        }

        public bool DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentLog = ReadAllEvents<TDataType>(data.Id);
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.DeleteLog(data, currentLog));
        }

        public bool UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentLog = ReadAllEvents<TDataType>(data.Id);
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.UndeleteLog(data, currentLog));
        }

        public bool RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentLog = ReadAllEvents<TDataType>(data.Id);
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),
                () => AuditGenerator.RollbackLog(data, currentLog));
        }

        public bool CommitEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(data.Id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    if (FileStreamer.CloseStream(filename))
                    {
                        FileStreamer.UnlockFile(filename);
                        return true;
                    }
                }
                catch
                {
                }
            }

            DiscardEvents(data);
            return false;
        }

        public void DiscardEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(data.Id);
            FileStreamer.DisposeOfStream(filename);
            FileStreamer.UnlockFile(filename);
        }

        public AuditLog ReadAllEvents<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                if (FileStreamer.GetLockForFile(filename))
                    try
                    {
                        var result = FileStreamer.ReadDataFromStream<AuditLog>(filename);
                        return result;
                    }
                    catch
                    {
                    }
                    finally
                    {
                        FileStreamer.UnlockFile(filename);
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

        private bool WriteEvent(string filename, int maxAttempts, Func<AuditLog> generateLog)
        {
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                if (FileStreamer.GetLockForFile(filename))
                    try
                    {
                        if (FileStreamer.WriteDataToStream(filename, generateLog.Invoke())) return true;
                        FileStreamer.UnlockFile(filename);
                    }
                    catch
                    {
                        FileStreamer.UnlockFile(filename);
                    }
            }

            return false;
        }
    }
}