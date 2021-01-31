using System;
using System.Linq;
using PackDB.Core.Auditing;
using PackDB.Core.Data;

namespace PackDB.FileSystem.AuditWorker
{
    public class FileAuditWorker : IFileAuditWorker
    {
        
        private IFileStreamer FileStreamer { get; set; }
        private IAuditGenerator AuditGenerator { get; set; }

        public FileAuditWorker(IFileStreamer fileStreamer, IAuditGenerator auditGenerator)
        {
            FileStreamer = fileStreamer;
            AuditGenerator = auditGenerator;
        }
        
        private static string GetFileName<TDataType>(int id)
        {
            return $"{typeof(TDataType).Name}\\{id}.audit";
        }

        private static int MaxAttempts<TDataType>()
        {
            var attributes = typeof(TDataType).GetCustomAttributes(typeof(AuditAttribute), true)
                .Cast<AuditAttribute>().ToArray();
            if (attributes.Any(x => x.MaxAttempts > 0))
            {
                return attributes.Max(x => x.MaxAttempts);
            }
            return -1;
        }

        private bool WriteEvent(string filename, int maxAttempts, Func<AuditLog> generateLog)
        {
            var attempts = 0;
            while (maxAttempts == -1 || attempts < maxAttempts)
            {
                attempts++;
                if (FileStreamer.GetLockForFile(filename))
                {
                    try
                    {
                        if (FileStreamer.WriteDataToStream(filename, generateLog.Invoke()))
                        {
                            return true;
                        }
                        FileStreamer.UnlockFile(filename);
                    }
                    catch
                    {
                        FileStreamer.UnlockFile(filename);
                    }
                }
            }
            return false;
        }
        
        public bool CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),() => AuditGenerator.NewLog(data));
        }

        public bool UpdateEvent<TDataType>(TDataType newData, TDataType oldData) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(newData.Id), MaxAttempts<TDataType>(),() => AuditGenerator.UpdateLog(newData,oldData,ReadAllEvents<TDataType>(newData.Id)));
        }

        public bool DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),() => AuditGenerator.DeleteLog(data,ReadAllEvents<TDataType>(data.Id)));
        }

        public bool UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),() => AuditGenerator.UndeleteLog(data,ReadAllEvents<TDataType>(data.Id)));
        }

        public bool RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            return WriteEvent(GetFileName<TDataType>(data.Id), MaxAttempts<TDataType>(),() => AuditGenerator.RollbackLog(data,ReadAllEvents<TDataType>(data.Id)));
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
                {
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
            }
            return null;
        }
    }
}