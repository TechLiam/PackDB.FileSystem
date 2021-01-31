using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PackDB.Core.Auditing;
using PackDB.Core.Data;

namespace PackDB.Core
{
    public class DataManager : IDataManager
    {
        
        private IDataWorker DataStreamer { get; }
        private IIndexWorker IndexWorker { get; }
        private IAuditWorker AuditWorker { get; }

        public DataManager(IDataWorker dataStreamer, IIndexWorker indexWorker, IAuditWorker auditWorker)
        {
            DataStreamer = dataStreamer;
            IndexWorker = indexWorker;
            AuditWorker = auditWorker;
        }
        
        public TDataType Read<TDataType>(int id) where TDataType : DataEntity
        {
            if (DataStreamer.Exists<TDataType>(id))
            {
                return DataStreamer.Read<TDataType>(id);
            }
            return null;
        }

        public IEnumerable<TDataType> Read<TDataType>(IEnumerable<int> ids) where TDataType : DataEntity
        {
            return ids.Select(Read<TDataType>);
        }

        public IEnumerable<TDataType> ReadIndex<TDataType, TKeyType>(TKeyType key, Expression<Func<TDataType,string>> indexProperty) where TDataType : DataEntity
        {
            var indexMember = ((MemberExpression) indexProperty.Body).Member;
            if (indexMember.IsDefined(typeof(IndexAttribute), true))
            {
                var indexName = indexMember.Name;
                if (IndexWorker.IndexExist(indexName))
                {
                    return Read<TDataType>(IndexWorker.GetIdsFromIndex(indexName, key));
                }
            }
            return null;
        }

        public bool Write<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentData = Read<TDataType>(data.Id);
            if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
            {
                var writtenData = DataStreamer.Write(data.Id, data);
                if (writtenData)
                {
                    var auditResult = currentData is null
                        ? AuditWorker.CreationEvent(data)
                        : AuditWorker.UpdateEvent(data, currentData);
                    if (auditResult)
                    {
                        writtenData = DataStreamer.Commit<TDataType>(data.Id);
                        if (writtenData)
                        {
                            var committedAudit = AuditWorker.CommitEvents(data);
                            if (committedAudit)
                            {
                                var indexed = IndexWorker.Index(data);
                                if (indexed)
                                {
                                    return true;
                                }
                                AuditWorker.RollbackEvent(data);
                            }
                            DataStreamer.Rollback(data.Id, currentData);
                            return false;
                        }
                        AuditWorker.DiscardEvents(data);
                        return false;
                    }
                    DataStreamer.DiscardChanges<TDataType>(data.Id);
                }
                return false;
            }
            var writeAndCommit = DataStreamer.WriteAndCommit(data.Id, data);
            if (writeAndCommit)
            {
                var indexed = IndexWorker.Index(data);
                if (!indexed)
                {
                    DataStreamer.Rollback(data.Id,currentData);
                    return false;
                }
            }
            return writeAndCommit;
        }

        public bool Delete<TDataType>(int id) where TDataType : DataEntity
        {
            var data = Read<TDataType>(id);
            if (data is null)
            {
                return false;
            }

            if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
            {
                if (AuditWorker.DeleteEvent(data))
                {
                    if (DataStreamer.Delete<TDataType>(id))
                    {
                        if (AuditWorker.CommitEvents(data))
                        {
                            if (IndexWorker.Unindex(data))
                            {
                                return true;
                            }

                            AuditWorker.RollbackEvent(data);
                        }

                        DataStreamer.Undelete<TDataType>(id);
                    }
                    else
                    {
                        AuditWorker.DiscardEvents(data);
                    }
                }

                return false;
            }

            if (DataStreamer.Delete<TDataType>(id))
            {
                if (IndexWorker.Unindex(data))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Restore<TDataType>(int id) where TDataType : DataEntity
        {
            var data = Read<TDataType>(id);
            if (data is null)
            {
                if (DataStreamer.Undelete<TDataType>(id))
                {
                    data = Read<TDataType>(id);
                    if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
                    {
                        if (AuditWorker.UndeleteEvent(data))
                        {
                            if (AuditWorker.CommitEvents(data))
                            {
                                if (IndexWorker.Index(data))
                                {
                                    return true;
                                }

                                AuditWorker.RollbackEvent(data);
                            }
                            else
                            {
                                AuditWorker.DiscardEvents(data);
                            }
                        }
                    }
                    else
                    {
                        if (IndexWorker.Index(data))
                        {
                            return true;
                        }
                    }
                    DataStreamer.Delete<TDataType>(id);
                }
                return false;
            }
            return true;
        }
    }
}