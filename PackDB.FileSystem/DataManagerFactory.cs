using System.Diagnostics.CodeAnalysis;
using PackDB.Core;
using PackDB.FileSystem.AuditWorker;
using PackDB.FileSystem.DataWorker;
using PackDB.FileSystem.IndexWorker;

namespace PackDB.FileSystem
{
    public static class DataManagerFactory
    {
        [ExcludeFromCodeCoverage]
        public static DataManager CreateFileSystemDataManager(string dataFolder = FileSystemConstants.DataFolder)
        {
            return new DataManager(new FileDataWorker(dataFolder), new FileIndexWorker(dataFolder),
                new FileAuditWorker(dataFolder));
        }
    }
}