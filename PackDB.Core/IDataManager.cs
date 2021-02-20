using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PackDB.Core.Data;

namespace PackDB.Core
{
    public interface IDataManager
    {
        Task<TDataType> Read<TDataType>(int id) where TDataType : DataEntity;
        IAsyncEnumerable<TDataType> Read<TDataType>(IEnumerable<int> ids) where TDataType : DataEntity;
        IAsyncEnumerable<TDataType> ReadIndex<TDataType, TKeyType>(TKeyType key, Expression<Func<TDataType, string>> indexProperty) where TDataType : DataEntity;
        Task<bool> Write<TDataType>(TDataType data) where TDataType : DataEntity;
        Task<bool> Delete<TDataType>(int id) where TDataType : DataEntity;
        Task<bool> Restore<TDataType>(int id) where TDataType : DataEntity;
        int GetNextId<TDataType>() where TDataType : DataEntity;
    }
}