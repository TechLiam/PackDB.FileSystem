using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using PackDB.Core.Data;

namespace PackDB.Core
{
    public interface IDataManager
    {
        TDataType Read<TDataType>(int id) where TDataType : DataEntity;
        IEnumerable<TDataType> Read<TDataType>(IEnumerable<int> ids) where TDataType : DataEntity;

        IEnumerable<TDataType> ReadIndex<TDataType, TKeyType>(TKeyType key,
            Expression<Func<TDataType, string>> indexProperty) where TDataType : DataEntity;

        bool Write<TDataType>(TDataType data) where TDataType : DataEntity;
        bool Delete<TDataType>(int id) where TDataType : DataEntity;
        bool Restore<TDataType>(int id) where TDataType : DataEntity;
        int GetNextId<TDataType>() where TDataType : DataEntity;
    }
}