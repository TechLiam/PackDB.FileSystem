namespace PackDB.Core.Data
{
    public interface IDataWorker
    {
        bool Write<TDataType>(int id, TDataType data) where TDataType : DataEntity;
        bool Commit<TDataType>(int id) where TDataType : DataEntity;
        void DiscardChanges<TDataType>(int id) where TDataType : DataEntity;
        bool WriteAndCommit<TDataType>(int id, TDataType data) where TDataType : DataEntity;
        TDataType Read<TDataType>(int id) where TDataType : DataEntity;
        bool Exists<TDataType>(int id) where TDataType : DataEntity;
        bool Delete<TDataType>(int id) where TDataType : DataEntity;
        bool Undelete<TDataType>(int id) where TDataType : DataEntity;
        void Rollback<TDataType>(int id, TDataType data) where TDataType : DataEntity;
    }
}