using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using PackDB.Core.Data;

namespace PackDB.Core.Auditing
{
    public class AuditGenerator : IAuditGenerator
    {
        private readonly ILogger _logger;

        public AuditGenerator() : this(new EmptyLogger())
        {
        }
        
        public AuditGenerator(ILogger logger)
        {
            using (logger.BeginScope("{Operation}", nameof(AuditGenerator)))
            {
                _logger = logger;
                _logger.LogInformation("Created audit generator");
            }
        }

        public AuditLog NewLog<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is creating {Action} for {DataType}", nameof(AuditGenerator),
                "a new entity log", typeof(TDataType).Name))
            {
                var properties = typeof(TDataType).GetProperties();
                var log = new AuditLog
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
                _logger.LogInformation("Created new entity log");
                return log;
            }
        }

        public AuditLog UpdateLog<TDataType, TOldDataType>(TDataType newData, TOldDataType oldData, AuditLog currentLog)
            where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is creating {Action} for {DataType}", nameof(AuditGenerator),
                "an update entity log", typeof(TDataType).Name))
            {
                var properties = typeof(TDataType).GetProperties();
                currentLog.Entries.Add(new AuditEntry
                {
                    Type = AuditType.Update,
                    Changes = properties
                        .Select(x => new AuditProperty(x.Name, x.GetValue(oldData), x.GetValue(newData)))
                        .ToImmutableArray()
                });
                _logger.LogInformation("Added update entity log");
                return currentLog;
            }
        }

        public AuditLog DeleteLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is creating {Action} for {DataType}", nameof(AuditGenerator),
                "a delete entity log", typeof(TDataType).Name))
            {
                var properties = typeof(TDataType).GetProperties();
                currentLog.Entries.Add(new AuditEntry
                {
                    Type = AuditType.Delete,
                    Changes = properties.Select(x => new AuditProperty(x.Name, x.GetValue(data), null))
                        .ToImmutableArray()
                });
                _logger.LogInformation("Added delete entity log");
                return currentLog;
            }
        }

        public AuditLog UndeleteLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is creating {Action} for {DataType}", nameof(AuditGenerator),
                "an undelete entity log", typeof(TDataType).Name))
            {
                var properties = typeof(TDataType).GetProperties();
                currentLog.Entries.Add(new AuditEntry
                {
                    Type = AuditType.Undelete,
                    Changes = properties.Select(x => new AuditProperty(x.Name, null, x.GetValue(data)))
                        .ToImmutableArray()
                });
                _logger.LogInformation("Added a undelete entity log");
                return currentLog;
            }
        }

        public AuditLog RollbackLog<TDataType>(TDataType data, AuditLog currentLog) where TDataType : DataEntity
        {
            using (_logger.BeginScope("{Operation} is creating {Action} for {DataType}", nameof(AuditGenerator),
                "a rollback entity log", typeof(TDataType).Name))
            {
                var properties = typeof(TDataType).GetProperties();
                currentLog.Entries.Add(new AuditEntry
                {
                    Type = AuditType.Rollback,
                    Changes = properties.Select(x => new AuditProperty(x.Name, null, x.GetValue(data)))
                        .ToImmutableArray()
                });
                _logger.LogInformation("Added a rollback entity log");
                return currentLog;
            }
        }
    }
}