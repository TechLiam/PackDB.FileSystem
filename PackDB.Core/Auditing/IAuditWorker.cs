using PackDB.Core.Data;

namespace PackDB.Core.Auditing
{
    public interface IAuditWorker
    {
        bool CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        bool UpdateEvent<TDataType>(TDataType newData, TDataType oldData) where TDataType : DataEntity;
        bool DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        bool UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        bool RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        bool CommitEvents<TDataType>(TDataType data) where TDataType : DataEntity;
        void DiscardEvents<TDataType>(TDataType data) where TDataType : DataEntity;
        AuditLog ReadAllEvents<TDataType>(int id) where TDataType : DataEntity;
    }
}