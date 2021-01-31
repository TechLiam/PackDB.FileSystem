using PackDB.Core.Data;

namespace PackDB.FileSystem.AuditWorker
{
    public class FileAuditWorker : IFileAuditWorker
    {
        public bool CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public bool UpdateEvent<TDataType>(TDataType newData, TDataType oldData) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public bool DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public bool UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public bool RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public bool CommitEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }

        public void DiscardEvents<TDataType>(TDataType data) where TDataType : DataEntity
        {
            throw new System.NotImplementedException();
        }
    }
}