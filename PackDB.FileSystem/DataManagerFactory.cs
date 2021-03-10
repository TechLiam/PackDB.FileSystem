using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PackDB.Core;
using PackDB.FileSystem.AuditWorker;
using PackDB.FileSystem.DataWorker;
using PackDB.FileSystem.IndexWorker;

namespace PackDB.FileSystem
{
    public static class DataManagerFactory
    {
        [ExcludeFromCodeCoverage]
        public static DataManager CreateFileSystemDataManager(ILogger logger)
        {
            return CreateFileSystemDataManager(FileSystemConstants.DataFolder, logger);
        }

        [ExcludeFromCodeCoverage]
        public static DataManager CreateFileSystemDataManager(string dataFolder = FileSystemConstants.DataFolder,
            ILogger logger = null)
        {
            logger ??= new EmptyLogger();
            return new DataManager(
                new FileDataWorker(dataFolder, logger),
                new FileIndexWorker(dataFolder, logger),
                new FileAuditWorker(dataFolder, logger),
                logger
            );
        }
    }
}