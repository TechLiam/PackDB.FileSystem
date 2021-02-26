using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackDB.Core.Auditing;
using PackDB.Core.Data;
using PackDB.Core.Indexing;

namespace PackDB.Core
{
    public class DataManager : IDataManager
    {
        public DataManager(IDataWorker dataStreamer, IIndexWorker indexWorker, IAuditWorker auditWorker, ILogger logger)
        {
            using (logger.BeginScope("{Operation}", nameof(DataManager)))
            {
                DataStreamer = dataStreamer;
                IndexWorker = indexWorker;
                AuditWorker = auditWorker;
                Logger = logger;
                Logger.LogInformation("Data manager created", dataStreamer, indexWorker, auditWorker);
            }
        }

        private IDataWorker DataStreamer { get; }
        private IIndexWorker IndexWorker { get; }
        private IAuditWorker AuditWorker { get; }
        private ILogger Logger { get; }

        public async Task<TDataType> Read<TDataType>(int id) where TDataType : DataEntity
        {
            using (Logger.BeginScope("{Operation} is {Action}ing {DataType} with Id ({Id})", nameof(DataManager), "read",typeof(TDataType).Name, id))
            {
                Logger.LogTrace("Started to read");
                if (await DataStreamer.Exists<TDataType>(id)) return await DataStreamer.Read<TDataType>(id);
                Logger.LogInformation("Didn't find the data");
                return null;
            }
        }

        public async IAsyncEnumerable<TDataType> Read<TDataType>(IEnumerable<int> ids) where TDataType : DataEntity
        {
            using (Logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(DataManager), "reading multiple",typeof(TDataType).Name, ids))
            {
                foreach (var id in ids) yield return await Read<TDataType>(id);
            }
        }

        public async IAsyncEnumerable<TDataType> ReadIndex<TDataType, TKeyType>(TKeyType key,
            Expression<Func<TDataType, string>> indexProperty) where TDataType : DataEntity
        {
            var indexMember = ((MemberExpression) indexProperty.Body).Member;
            using (Logger.BeginScope("{Operation} is {Action} {IndexName} for {DataType} with a key {key}", nameof(DataManager), "read from index", indexMember.Name, typeof(TDataType).Name, key))
            {
                if (indexMember.IsDefined(typeof(IndexAttribute), true))
                {
                    Logger.LogTrace("Property is indexed");
                    var indexName = indexMember.Name;
                    if (await IndexWorker.IndexExist<TDataType>(indexName))
                    {
                        Logger.LogTrace("Index exists");
                        var ids = IndexWorker.GetIdsFromIndex<TDataType, TKeyType>(indexName, key);
                        Logger.LogInformation("Found ids in index", ids, key);
                        await foreach (var id in ids) yield return await Read<TDataType>(id);
                    }
                    yield break;
                }
                Logger.LogWarning("The property used is not marked as indexed");
            }
        }

        public async Task<bool> Write<TDataType>(TDataType data) where TDataType : DataEntity
        {
            using (Logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(DataManager), "writing", typeof(TDataType).Name, data.Id))
            {
                Logger.LogTrace("Checking if data exists");
                var currentData = await Read<TDataType>(data.Id);
                Logger.LogInformation(currentData is null ? "Creating data" : "Updating data");
                if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
                {
                    Logger.LogTrace("{DataType} is audited", typeof(TDataType).Name);
                    var writtenData = await DataStreamer.Write(data.Id, data);
                    if (writtenData)
                    {
                        Logger.LogTrace("Data ready to commit to store");
                        var auditResult = currentData is null
                            ? await AuditWorker.CreationEvent(data)
                            : await AuditWorker.UpdateEvent(data, currentData);
                        if (auditResult)
                        {
                            Logger.LogTrace("Audit record created");
                            writtenData = await DataStreamer.Commit<TDataType>(data.Id);
                            if (writtenData)
                            {
                                Logger.LogTrace("Data saved to store");
                                var committedAudit = await AuditWorker.CommitEvents(data);
                                if (committedAudit)
                                {
                                    Logger.LogTrace("Audit saved to store");
                                    var indexed = await IndexWorker.Index(data);
                                    if (indexed)
                                    {
                                        Logger.LogInformation("Data, Audit saved successfully");
                                        return true;
                                    }
                                    Logger.LogWarning("Failed to index data");
                                    await AuditWorker.RollbackEvent(data);
                                    Logger.LogInformation("Added rollback to audit");
                                    Logger.LogTrace("Rolled back data");
                                }

                                await DataStreamer.Rollback(data.Id, currentData);
                                Logger.LogInformation("Rolled data back");
                                return false;
                            }

                            await AuditWorker.DiscardEvents(data);
                            return false;
                        }

                        Logger.LogWarning("Failed to create audit record for data");
                        await DataStreamer.DiscardChanges<TDataType>(data.Id);
                        Logger.LogTrace("Discard changes");
                    }
                    Logger.LogWarning("Failed to save data to the store");
                    return false;
                }
                
                Logger.LogTrace("Data is not audited");
                var writeAndCommit = await DataStreamer.WriteAndCommit(data.Id, data);
                if (writeAndCommit)
                {
                    var indexed = await IndexWorker.Index(data);
                    if (!indexed)
                    {
                        Logger.LogWarning("Failed to index data");
                        await DataStreamer.Rollback(data.Id, currentData);
                        Logger.LogInformation("Rolled data back");
                        return false;
                    }
                    Logger.LogTrace("Data was indexed");
                    Logger.LogInformation("Data saved successfully");
                }
                else
                {
                    Logger.LogWarning("Failed to save data");
                }
                return writeAndCommit;
            }
        }

        public async Task<bool> Delete<TDataType>(int id) where TDataType : DataEntity
        {
            using (Logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(DataManager), "deleteing", typeof(TDataType).Name, id))
            {
                var data = await Read<TDataType>(id);
                if (data is null)
                {
                    Logger.LogWarning("Unable to find the data it may already be deleted");
                    return false;
                }
                
                if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
                {
                    Logger.LogTrace("Data is audited");
                    if (await AuditWorker.DeleteEvent(data))
                    {
                        Logger.LogTrace("Delete audit record created");
                        if (await DataStreamer.Delete<TDataType>(id))
                        {
                            Logger.LogTrace("Data deleted from store");
                            if (await AuditWorker.CommitEvents(data))
                            {
                                Logger.LogTrace("Audit committed to store");
                                if (await IndexWorker.Unindex(data))
                                {
                                    Logger.LogInformation("Data deleted from store");
                                    return true;
                                }
                                Logger.LogWarning("Failed to remove indexes for data");
                                await AuditWorker.RollbackEvent(data);
                                Logger.LogInformation("Added rollback audit record");
                            }
                            else
                            {
                                Logger.LogWarning("Failed to commit audit to store");
                            }
                            await DataStreamer.Rollback(id,data);
                            Logger.LogInformation("Rolled data back");
                        }
                        else
                        {
                            Logger.LogWarning("Failed to delete data");
                            await AuditWorker.DiscardEvents(data);
                            Logger.LogTrace("Discarded audit record");
                        }
                        return false;
                    }
                    Logger.LogWarning("Failed to create audit record for delete");
                    return false;
                }

                Logger.LogTrace("Data is not audited");
                if (await DataStreamer.Delete<TDataType>(id))
                {
                    Logger.LogInformation("Deleted data");
                    if (await IndexWorker.Unindex(data))
                    {
                        Logger.LogInformation("Removed any indexes for data");
                        return true;
                    }
                    Logger.LogWarning("Failed to remove indexes for data");
                    await DataStreamer.Rollback(id, data);
                    Logger.LogInformation("Rolled data back");
                }
                Logger.LogWarning("Failed to delete data");
                return false;
            }
        }

        public async Task<bool> Restore<TDataType>(int id) where TDataType : DataEntity
        {
            using (Logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(DataManager), "restoring", typeof(TDataType).Name, id))
            {
                var data = await Read<TDataType>(id);
                if (data is null)
                {
                    Logger.LogTrace("Data could be deleted");
                    if (await DataStreamer.Undelete<TDataType>(id))
                    {
                        Logger.LogTrace("Data restored to store");
                        data = await Read<TDataType>(id);
                        if (typeof(TDataType).IsDefined(typeof(AuditAttribute), true))
                        {
                            Logger.LogTrace("Data is audited");
                            if (await AuditWorker.UndeleteEvent(data))
                            {
                                if (await AuditWorker.CommitEvents(data))
                                {
                                    Logger.LogTrace("Audit committed to store");
                                    if (await IndexWorker.Index(data))
                                    {
                                        Logger.LogInformation("Restored data with audit");
                                        return true;
                                    }
                                    Logger.LogWarning("Failed to index data");
                                    await AuditWorker.RollbackEvent(data);
                                }
                                else
                                {
                                    Logger.LogWarning("Failed to commit audit for data to store");
                                    await AuditWorker.DiscardEvents(data);
                                    Logger.LogTrace("Discarded audit record");
                                }
                            }
                            else
                            {
                                Logger.LogWarning("Failed to create undelete audit record for data");
                            }
                        }
                        else
                        {
                            if (await IndexWorker.Index(data))
                            {
                                Logger.LogInformation("Restored data");
                                return true;
                            }
                            Logger.LogWarning("Failed to index data");
                        }
                        await DataStreamer.Delete<TDataType>(id);
                        Logger.LogInformation("Rolled back restore");
                    }
                    Logger.LogWarning("Failed to restore data");
                    return false;
                }
                Logger.LogWarning("Data didn't need restoring");
                return true;
            }
        }

        public int GetNextId<TDataType>() where TDataType : DataEntity
        {
            using (Logger.BeginScope("{Operation} is {Action} {DataType} with Id ({Id})", nameof(DataManager), "getting next id", typeof(TDataType).Name))
            {
                Logger.LogTrace("Getting next id from store");
                var id = DataStreamer.NextId<TDataType>();
                Logger.LogInformation("Next id is {id}", id);
                return id;
            }
        }
    }
}