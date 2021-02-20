using System.Threading.Tasks;
using PackDB.Core.Data;

namespace PackDB.Core.Auditing
{
    public interface IAuditWorker
    {
        Task<bool> CreationEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<bool> UpdateEvent<TDataType>(TDataType newData, TDataType oldData) where TDataType : DataEntity;
        Task<bool> DeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<bool> UndeleteEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<bool> RollbackEvent<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<bool> CommitEvents<TDataType>(TDataType data) where TDataType : DataEntity;
        Task DiscardEvents<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<AuditLog> ReadAllEvents<TDataType>(int id) where TDataType : DataEntity;
    }
}