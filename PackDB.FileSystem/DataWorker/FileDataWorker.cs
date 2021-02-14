using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PackDB.Core.Data;
using PackDB.FileSystem.Attributes;

namespace PackDB.FileSystem.DataWorker
{
    public class FileDataWorker : IFileDataWorker
    {
        [ExcludeFromCodeCoverage]
        public FileDataWorker() : this(new FileStreamer())
        {
        }

        [ExcludeFromCodeCoverage]
        public FileDataWorker(string dataFolder) : this(new FileStreamer(),dataFolder)
        {
        }

        public FileDataWorker(IFileStreamer fileStreamer, string dataFolder = FileSystemConstants.DataFolder)
        {
            FileStreamer = fileStreamer;
            TopLevelDataFolderName = dataFolder;
        }

        private IFileStreamer FileStreamer { get; }

        private string TopLevelDataFolderName { get; }
        
        public bool Write<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                if (FileStreamer.GetLockForFile(filename))
                    try
                    {
                        if (FileStreamer.WriteDataToStream(filename, data)) return true;
                        FileStreamer.UnlockFile(filename);
                    }
                    catch
                    {
                        FileStreamer.UnlockFile(filename);
                    }
            }

            return false;
        }

        public bool Commit<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (attempts < maxAttempts)
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

            DiscardChanges<TDataType>(id);
            return false;
        }

        public void DiscardChanges<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            FileStreamer.DisposeOfStream(filename);
            FileStreamer.UnlockFile(filename);
        }

        public bool WriteAndCommit<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            return Write(id, data) && Commit<TDataType>(id);
        }

        public TDataType Read<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                if (FileStreamer.GetLockForFile(filename))
                    try
                    {
                        var result = FileStreamer.ReadDataFromStream<TDataType>(filename);
                        return result;
                    }
                    catch
                    {
                    }
                    finally
                    {
                        FileStreamer.UnlockFile(filename);
                        FileStreamer.CloseStream(filename);
                    }
            }

            return null;
        }

        public bool Exists<TDataType>(int id) where TDataType : DataEntity
        {
            return FileStreamer.Exists(GetFileName<TDataType>(id));
        }

        public bool Delete<TDataType>(int id) where TDataType : DataEntity
        {
            return IsSoftDelete<TDataType>()
                ? FileStreamer.SoftDelete(GetFileName<TDataType>(id))
                : FileStreamer.Delete(GetFileName<TDataType>(id));
        }

        public bool Undelete<TDataType>(int id) where TDataType : DataEntity
        {
            return IsSoftDelete<TDataType>() && FileStreamer.Undelete(GetFileName<TDataType>(id));
        }

        public void Rollback<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            while (!WriteAndCommit(id, data))
            {
            }
        }

        public int NextId<TDataType>() where TDataType : DataEntity
        {
            var files = FileStreamer.GetAllFileNames(GetFolderName<TDataType>(), "data");
            return files.Any() ? Math.Max(files.Max(int.Parse) + 1, 1) : 1;
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