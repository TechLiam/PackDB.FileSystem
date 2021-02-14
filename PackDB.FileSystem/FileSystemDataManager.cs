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
        public static DataManager CreateFileSystemDataManager()
        {
            return new DataManager(new FileDataWorker(), new FileIndexWorker(), new FileAuditWorker());
        }
    }
}