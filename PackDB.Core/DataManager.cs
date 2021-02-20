using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PackDB.Core.Auditing;
using PackDB.Core.Data;
using PackDB.Core.Indexing;

namespace PackDB.Core
{
    public class DataManager : IDataManager
    {
        public DataManager(IDataWorker dataStreamer, IIndexWorker indexWorker, IAuditWorker auditWorker)
        {
            DataStreamer = dataStreamer;
            IndexWorker = indexWorker;
            AuditWorker = auditWorker;
        }

        private IDataWorker DataStreamer { get; }
        private IIndexWorker IndexWorker { get; }
        private IAuditWorker AuditWorker { get; }

        public async Task<TDataType> Read<TDataType>(int id) where TDataType : DataEntity
        {
            if (await DataStreamer.Exists<TDataType>(id)) return await DataStreamer.Read<TDataType>(id);
            return null;
        }

        public async IAsyncEnumerable<TDataType> Read<TDataType>(IEnumerable<int> ids) where TDataType : DataEntity
        {
            foreach (var id in ids)
            {
                yield return await Read<TDataType>(id);
            }
        }

        public async IAsyncEnumerable<TDataType> ReadIndex<TDataType, TKeyType>(TKeyType key, Expression<Func<TDataType, string>> indexProperty) where TDataType : DataEntity
        {
            var indexMember = ((MemberExpression) indexProperty.Body).Member;
            if (indexMember.IsDefined(typeof(IndexAttribute), true))
            {
                var indexName = indexMember.Name;
                if (await IndexWorker.IndexExist<TDataType>(indexName))
                {
                    var ids = IndexWorker.GetIdsFromIndex<TDataType, TKeyType>(indexName, key);
                    await foreach (var id in ids)
                    {
                        yield return await Read<TDataType>(id);
                    }
                }
            }
        }

        public async Task<bool> Write<TDataType>(TDataType data) where TDataType : DataEntity
        {
            var currentData = await Read<TDataType>(data.Id);
            if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
            {
                var writtenData = await DataStreamer.Write(data.Id, data);
                if (writtenData)
                {
                    var auditResult = currentData is null
                        ? await AuditWorker.CreationEvent(data)
                        : await AuditWorker.UpdateEvent(data, currentData);
                    if (auditResult)
                    {
                        writtenData = await DataStreamer.Commit<TDataType>(data.Id);
                        if (writtenData)
                        {
                            var committedAudit = await AuditWorker.CommitEvents(data);
                            if (committedAudit)
                            {
                                var indexed = await IndexWorker.Index(data);
                                if (indexed) return true;
                                await AuditWorker.RollbackEvent(data);
                            }

                            await DataStreamer.Rollback(data.Id, currentData);
                            return false;
                        }

                        await AuditWorker.DiscardEvents(data);
                        return false;
                    }

                    await DataStreamer.DiscardChanges<TDataType>(data.Id);
                }

                return false;
            }

            var writeAndCommit = await DataStreamer.WriteAndCommit(data.Id, data);
            if (writeAndCommit)
            {
                var indexed = await IndexWorker.Index(data);
                if (!indexed)
                {
                    await DataStreamer.Rollback(data.Id, currentData);
                    return false;
                }
            }

            return writeAndCommit;
        }

        public async Task<bool> Delete<TDataType>(int id) where TDataType : DataEntity
        {
            var data = await Read<TDataType>(id);
            if (data is null) return false;

            if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
            {
                if (await AuditWorker.DeleteEvent(data))
                {
                    if (await DataStreamer.Delete<TDataType>(id))
                    {
                        if (await AuditWorker.CommitEvents(data))
                        {
                            if (await IndexWorker.Unindex(data)) return true;

                            await AuditWorker.RollbackEvent(data);
                        }

                        await DataStreamer.Undelete<TDataType>(id);
                    }
                    else
                    {
                        await AuditWorker.DiscardEvents(data);
                    }
                }

                return false;
            }

            if (await DataStreamer.Delete<TDataType>(id))
                if (await IndexWorker.Unindex(data))
                    return true;

            return false;
        }

        public async Task<bool> Restore<TDataType>(int id) where TDataType : DataEntity
        {
            var data = await Read<TDataType>(id);
            if (data is null)
            {
                if (await DataStreamer.Undelete<TDataType>(id))
                {
                    data = await Read<TDataType>(id);
                    if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
                    {
                        if (await AuditWorker.UndeleteEvent(data))
                        {
                            if (await AuditWorker.CommitEvents(data))
                            {
                                if (await IndexWorker.Index(data)) return true;

                                await AuditWorker.RollbackEvent(data);
                            }
                            else
                            {
                                await AuditWorker.DiscardEvents(data);
                            }
                        }
                    }
                    else
                    {
                        if (await IndexWorker.Index(data)) return true;
                    }

                    await DataStreamer.Delete<TDataType>(id);
                }

                return false;
            }

            return true;
        }

        public int GetNextId<TDataType>() where TDataType : DataEntity
        {
            return DataStreamer.NextId<TDataType>();
        }
    }
}