using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using PackDB.Core.Data;

namespace PackDB.Core.Auditing
{
    public class AuditGenerator : IAuditGenerator
    {
        private ILogger _logger;

        public AuditGenerator(ILogger logger)
        {
            _logger = logger;
        }

        public AuditLog NewLog<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var properties = typeof(TDataType).GetProperties();
            return new AuditLog
            {
                Entries = new List<AuditEntry>
                {
                    new AuditEntry
                    {
                        Type = AuditType.Create,
                        Changes = properties.Select(x => new AuditProperty(x.Name, null, x.GetValue(data)))
                            .ToImmutableArray()
                    }
                }
            };
        }

        public AuditLog UpdateLog<TDataType, TOldDataType>(TDataType newData, TOldDataType oldData, AuditLog currentLog)
            where TDataType : DataEntity
        {
            var properties = typeof(TDataType).GetProperties();
            currentLog.Entries.Add(new AuditEntry
            {
                Type = AuditType.Update,
                Changes = properties.Select(x => new AuditProperty(x.Name, x.GetValue(oldData), x.GetValue(newData)))
                    .ToImmutableArray()
            });
            return currentLog;
        }

        public AuditLog DeleteLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity
        {
            var properties = typeof(TDataType).GetProperties();
            currentLog.Entries.Add(new AuditEntry
            {
                Type = AuditType.Delete,
                Changes = properties.Select(x => new AuditProperty(x.Name, x.GetValue(data), null)).ToImmutableArray()
            });
            return currentLog;
        }

        public AuditLog UndeleteLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity
        {
            var properties = typeof(TDataType).GetProperties();
            currentLog.Entries.Add(new AuditEntry
            {
                Type = AuditType.Undelete,
                Changes = properties.Select(x => new AuditProperty(x.Name, null, x.GetValue(data))).ToImmutableArray()
            });
            return currentLog;
        }

        public AuditLog RollbackLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity
        {
            var properties = typeof(TDataType).GetProperties();
            currentLog.Entries.Add(new AuditEntry
            {
                Type = AuditType.Rollback,
                Changes = properties.Select(x => new AuditProperty(x.Name, null, x.GetValue(data))).ToImmutableArray()
            });
            return currentLog;
        }
    }
}