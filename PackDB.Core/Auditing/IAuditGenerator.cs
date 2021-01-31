using PackDB.Core.Data;

namespace PackDB.Core.Auditing
{
    public interface IAuditGenerator
    {
        AuditLog NewLog<TDataType>(TDataType data) where TDataType : DataEntity;
        AuditLog UpdateLog<TDataType, TOldDataType>(TDataType newData, TOldDataType oldData, AuditLog currentLog) where TDataType : DataEntity;
        AuditLog DeleteLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity;
        AuditLog UndeleteLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity;
        AuditLog RollbackLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity;
    }
}