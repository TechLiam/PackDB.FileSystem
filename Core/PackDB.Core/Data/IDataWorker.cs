using System.Threading.Tasks;

namespace PackDB.Core.Data
{
    public interface IDataWorker
    {
        Task<bool> Write<TDataType>(int id, TDataType data) where TDataType : DataEntity;
        Task<bool> Commit<TDataType>(int id) where TDataType : DataEntity;
        Task DiscardChanges<TDataType>(int id) where TDataType : DataEntity;
        Task<bool> WriteAndCommit<TDataType>(int id, TDataType data) where TDataType : DataEntity;
        Task<TDataType> Read<TDataType>(int id) where TDataType : DataEntity;
        Task<bool> Exists<TDataType>(int id) where TDataType : DataEntity;
        Task<bool> Delete<TDataType>(int id) where TDataType : DataEntity;
        Task<bool> Undelete<TDataType>(int id) where TDataType : DataEntity;
        Task Rollback<TDataType>(int id, TDataType data) where TDataType : DataEntity;
        int NextId<TDataType>() where TDataType : DataEntity;
    }
}