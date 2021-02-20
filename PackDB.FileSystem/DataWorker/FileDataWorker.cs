using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
        public FileDataWorker(string dataFolder) : this(new FileStreamer(), dataFolder)
        {
        }

        public FileDataWorker(IFileStreamer fileStreamer, string dataFolder = FileSystemConstants.DataFolder)
        {
            FileStreamer = fileStreamer;
            TopLevelDataFolderName = dataFolder;
        }

        private IFileStreamer FileStreamer { get; }

        private string TopLevelDataFolderName { get; }

        public async Task<bool> Write<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                if (await FileStreamer.GetLockForFile(filename))
                    try
                    {
                        if (await FileStreamer.WriteDataToStream(filename, data)) return true;
                        await FileStreamer.UnlockFile(filename);
                    }
                    catch
                    {
                        await FileStreamer.UnlockFile(filename);
                    }
            }

            return false;
        }

        public async Task<bool> Commit<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (attempts < maxAttempts)
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

            await DiscardChanges<TDataType>(id);
            return false;
        }

        public async Task DiscardChanges<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            await FileStreamer.DisposeOfStream(filename);
            await FileStreamer.UnlockFile(filename);
        }

        public async Task<bool> WriteAndCommit<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            return await Write(id, data) && await Commit<TDataType>(id);
        }

        public async Task<TDataType> Read<TDataType>(int id) where TDataType : DataEntity
        {
            var filename = GetFileName<TDataType>(id);
            var maxAttempts = MaxAttempts<TDataType>();
            var attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                if (await FileStreamer.GetLockForFile(filename))
                    try
                    {
                        return await FileStreamer.ReadDataFromStream<TDataType>(filename);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        await FileStreamer.UnlockFile(filename);
                        await FileStreamer.CloseStream(filename);
                    }
            }

            return null;
        }

        public Task<bool> Exists<TDataType>(int id) where TDataType : DataEntity
        {
            return FileStreamer.Exists(GetFileName<TDataType>(id));
        }

        public Task<bool> Delete<TDataType>(int id) where TDataType : DataEntity
        {
            return IsSoftDelete<TDataType>()
                ? FileStreamer.SoftDelete(GetFileName<TDataType>(id))
                : FileStreamer.Delete(GetFileName<TDataType>(id));
        }

        public Task<bool> Undelete<TDataType>(int id) where TDataType : DataEntity
        {
            return IsSoftDelete<TDataType>()
                ? FileStreamer.Undelete(GetFileName<TDataType>(id))
                : Task.FromResult(false);
        }

        public async Task Rollback<TDataType>(int id, TDataType data) where TDataType : DataEntity
        {
            while (!await WriteAndCommit(id, data))
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