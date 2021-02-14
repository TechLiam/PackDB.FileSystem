using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Auditing;
using PackDB.Core.Data;
using PackDB.Core.Indexing;

namespace PackDB.Core.Tests
{
    [TestFixture]
    [TestOf(typeof(DataManager))]
    [ExcludeFromCodeCoverage]
    public class DataManagerTester
    {
        [SetUp]
        public void Setup()
        {
            Randomizer = Randomizer.CreateRandomizer();

            ExpectedBasicEntity = new BasicEntity {Id = Randomizer.Next()};
            ExpectedIndexedEntity = new IndexedEntity {Id = Randomizer.Next(), IndexedValue = Randomizer.GetString()};
            ExpectedAuditedEntity = new AuditedEntity {Id = Randomizer.Next()};

            MockDataStream = new Mock<IDataWorker>();
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(ExpectedBasicEntity);
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedIndexedEntity.Id))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedIndexedEntity.Id))
                .Returns(ExpectedIndexedEntity);
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedAuditedEntity.Id))
                .Returns(ExpectedAuditedEntity);
            MockDataStream
                .Setup(x => x.WriteAndCommit(ExpectedBasicEntity.Id, ExpectedBasicEntity))
                .Returns(true);
            MockDataStream
                .Setup(x => x.WriteAndCommit(ExpectedIndexedEntity.Id, ExpectedIndexedEntity))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Write(ExpectedAuditedEntity.Id, ExpectedAuditedEntity))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Commit<BasicEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Delete<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(true);
            MockDataStream
                .Setup(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            ExpectedNextBasicEntityId = Randomizer.Next();
            MockDataStream
                .Setup(x => x.NextId<BasicEntity>())
                .Returns(ExpectedNextBasicEntityId);

            MockIndexer = new Mock<IIndexWorker>();
            MockIndexer
                .Setup(x => x.IndexExist<IndexedEntity>("IndexedValue"))
                .Returns(true);
            MockIndexer
                .Setup(
                    x => x.GetIdsFromIndex<IndexedEntity, string>("IndexedValue", ExpectedIndexedEntity.IndexedValue))
                .Returns(new List<int> {ExpectedIndexedEntity.Id});
            MockIndexer
                .Setup(x => x.Index(ExpectedBasicEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedIndexedEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Unindex(ExpectedBasicEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Unindex(ExpectedAuditedEntity))
                .Returns(true);

            MockAudit = new Mock<IAuditWorker>();
            MockAudit
                .Setup(x => x.CreationEvent(ExpectedAuditedEntity))
                .Returns(true);
            MockAudit
                .Setup(x => x.UpdateEvent(ExpectedAuditedEntity, ExpectedAuditedEntity))
                .Returns(true);
            MockAudit
                .Setup(x => x.DeleteEvent(ExpectedAuditedEntity))
                .Returns(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(true);

            DataManager = new DataManager(MockDataStream.Object, MockIndexer.Object, MockAudit.Object);
        }

        private DataManager DataManager { get; set; }
        private static BasicEntity ExpectedBasicEntity { get; set; }
        private static IndexedEntity ExpectedIndexedEntity { get; set; }
        private static AuditedEntity ExpectedAuditedEntity { get; set; }
        private static int ExpectedNextBasicEntityId { get; set; }
        private Randomizer Randomizer { get; set; }
        private Mock<IDataWorker> MockDataStream { get; set; }
        private Mock<IIndexWorker> MockIndexer { get; set; }
        private Mock<IAuditWorker> MockAudit { get; set; }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenThereNoData()
        {
            Assert.IsNull(DataManager.Read<BasicEntity>(Randomizer.Next()));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenThereIsData()
        {
            Assert.AreSame(ExpectedBasicEntity, DataManager.Read<BasicEntity>(ExpectedBasicEntity.Id));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadMultipleWhenThereNoData()
        {
            Assert.IsFalse(DataManager.Read<BasicEntity>(new[] {Randomizer.Next(), Randomizer.Next()})
                .Any(y => y != null));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadMultipleWhenThereIsData()
        {
            var results = DataManager.Read<BasicEntity>(new[] {ExpectedBasicEntity.Id, ExpectedIndexedEntity.Id})
                .ToArray();
            Assert.AreEqual(2, results.Count());
            Assert.AreSame(ExpectedBasicEntity, results.ElementAt(0));
            Assert.AreSame(ExpectedIndexedEntity, results.ElementAt(1));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenIndexPropertyIsNotIndexed()
        {
            Assert.IsNull(DataManager.ReadIndex<BasicEntity, string>(ExpectedBasicEntity.Value1, x => x.Value1));
            MockIndexer.Verify(x => x.IndexExist<BasicEntity>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenIndexDoesNotExist()
        {
            MockIndexer.Setup(x => x.IndexExist<IndexedEntity>("IndexedValue")).Returns(false);
            Assert.IsNull(
                DataManager.ReadIndex<IndexedEntity, string>(ExpectedBasicEntity.Value1, x => x.IndexedValue));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenIndexHasNoValue()
        {
            MockIndexer
                .Setup(
                    x => x.GetIdsFromIndex<IndexedEntity, string>("IndexedValue", ExpectedIndexedEntity.IndexedValue))
                .Returns(new List<int>());
            Assert.IsFalse(DataManager
                .ReadIndex<IndexedEntity, string>(ExpectedIndexedEntity.IndexedValue, x => x.IndexedValue).Any());
        }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenIndexHasAValueButThereNoData()
        {
            MockIndexer
                .Setup(
                    x => x.GetIdsFromIndex<IndexedEntity, string>("IndexedValue", ExpectedIndexedEntity.IndexedValue))
                .Returns(new List<int> {Randomizer.Next()});
            Assert.IsFalse(DataManager
                .ReadIndex<IndexedEntity, string>(ExpectedIndexedEntity.IndexedValue, x => x.IndexedValue)
                .Any(x => x != null));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadWhenIndexHasAValueAndThereIsData()
        {
            var result = DataManager
                .ReadIndex<IndexedEntity, string>(ExpectedIndexedEntity.IndexedValue, x => x.IndexedValue).ToArray();
            Assert.AreEqual(result.Count(), 1);
            Assert.AreSame(ExpectedIndexedEntity, result.ElementAt(0));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithNoAuditFails()
        {
            MockDataStream
                .Setup(x => x.WriteAndCommit(It.IsAny<int>(), It.IsAny<DataEntity>()))
                .Returns(false);
            var result = DataManager.Write(ExpectedBasicEntity);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithNoAuditIndexingFails()
        {
            MockIndexer
                .Setup(x => x.Index(It.IsAny<DataEntity>()))
                .Returns(false);
            var result = DataManager.Write(ExpectedIndexedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedIndexedEntity.Id, ExpectedIndexedEntity));
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteNewDataWithNoAuditIndexingFails()
        {
            MockDataStream
                .Setup(x => x.Read<BasicEntity>(ExpectedIndexedEntity.Id))
                .Returns((BasicEntity) null);
            MockIndexer
                .Setup(x => x.Index(It.IsAny<DataEntity>()))
                .Returns(false);
            var result = DataManager.Write(ExpectedIndexedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedIndexedEntity.Id, (DataEntity) null));
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteDataWithNoAuditOrIndexSuccess()
        {
            var result = DataManager.Write(ExpectedBasicEntity);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteDataWithNoAuditButIndexSuccess()
        {
            var result = DataManager.Write(ExpectedIndexedEntity);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithAuditWriteFails()
        {
            MockDataStream
                .Setup(x => x.Write(It.IsAny<int>(), It.IsAny<DataEntity>()))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Commit<BasicEntity>(It.IsAny<int>()), Times.Never);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithAuditCreateEventFails()
        {
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            MockAudit
                .Setup(x => x.CreationEvent(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.DiscardChanges<BasicEntity>(ExpectedAuditedEntity.Id));
            MockDataStream
                .Verify(x => x.Commit<BasicEntity>(It.IsAny<int>()), Times.Never);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithAuditUpdateEventFails()
        {
            MockAudit
                .Setup(x => x.UpdateEvent(ExpectedAuditedEntity, ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.DiscardChanges<BasicEntity>(ExpectedAuditedEntity.Id));
            MockDataStream
                .Verify(x => x.Commit<BasicEntity>(It.IsAny<int>()), Times.Never);
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteNewDataWithAuditCommitFails()
        {
            MockDataStream
                .Setup(x => x.Commit<BasicEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity));
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithAuditCommitFails()
        {
            MockDataStream
                .Setup(x => x.Commit<BasicEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity));
            MockDataStream
                .Verify(x => x.Rollback(It.IsAny<int>(), It.IsAny<DataEntity>()), Times.Never);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CommitEvents(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteNewDataWithAuditWhenAuditCommitFails()
        {
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(false);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, (DataEntity) null), Times.Once);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithAuditWhenAuditCommitFails()
        {
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Once);
            MockIndexer
                .Verify(x => x.Index(It.IsAny<DataEntity>()), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteNewDataWithAuditWhenIndexingFails()
        {
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .Returns(false);
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, (DataEntity) null), Times.Once);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteDataWithAuditWhenIndexingFails()
        {
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Once);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteNewDataWithAuditWhenIndexingSuccess()
        {
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, (DataEntity) null), Times.Never);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Never);
            MockAudit
                .Verify(x => x.UpdateEvent(It.IsAny<DataEntity>(), It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteDataWithAuditWhenIndexingSuccess()
        {
            var result = DataManager.Write(ExpectedAuditedEntity);
            MockDataStream
                .Verify(x => x.Rollback(ExpectedAuditedEntity.Id, ExpectedAuditedEntity), Times.Never);
            MockIndexer
                .Verify(x => x.Index(ExpectedAuditedEntity), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Never);
            MockAudit
                .Verify(x => x.CreationEvent(It.IsAny<DataEntity>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWhenItDoesNotExist()
        {
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(false);
            return DataManager.Delete<BasicEntity>(ExpectedBasicEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWhenDeleteFails()
        {
            MockDataStream
                .Setup(x => x.Delete<DataEntity>(ExpectedBasicEntity.Id))
                .Returns(false);
            return DataManager.Delete<BasicEntity>(ExpectedBasicEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataUnindexFails()
        {
            MockIndexer
                .Setup(x => x.Unindex(ExpectedBasicEntity))
                .Returns(false);
            return DataManager.Delete<BasicEntity>(ExpectedBasicEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool DeleteDataSuccess()
        {
            return DataManager.Delete<BasicEntity>(ExpectedBasicEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWithAuditWhenItDoesNotExist()
        {
            MockDataStream
                .Setup(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.DeleteEvent(ExpectedAuditedEntity), Times.Never);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(It.IsAny<int>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWithAuditDeleteEventFails()
        {
            MockAudit
                .Setup(x => x.DeleteEvent(It.IsAny<AuditedEntity>()))
                .Returns(false);
            var result = DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(It.IsAny<int>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWithAuditDeleteFails()
        {
            MockDataStream
                .Setup(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false);
            var result = DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.CommitEvents(ExpectedAuditedEntity), Times.Never);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWithAuditCommitEventFails()
        {
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteDataWithAuditUnindexFails()
        {
            MockIndexer
                .Setup(x => x.Unindex(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool DeleteDataWithAuditDeleteSuccessful()
        {
            var result = DataManager.Delete<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool RestoreDataWhenItExists()
        {
            return DataManager.Restore<BasicEntity>(ExpectedBasicEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RestoreDataWhenDoesNotExistButUndeleteFails()
        {
            MockDataStream
                .Setup(x => x.Exists<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(false);
            MockDataStream
                .Setup(x => x.Undelete<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(false);
            return DataManager.Restore<BasicEntity>(ExpectedBasicEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RestoreDataWithAuditingWhenDoesNotExistButUndeleteEventFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false)
                .Returns(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RestoreDataWithAuditingWhenDoesNotExistButCommitEventFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false)
                .Returns(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .Returns(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.DiscardEvents(ExpectedAuditedEntity), Times.Once);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RestoreDataWithAuditingWhenDoesNotExistButIndexingFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false)
                .Returns(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .Returns(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .Returns(false);
            var result = DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
            MockAudit
                .Verify(x => x.RollbackEvent(ExpectedAuditedEntity), Times.Once);
            MockDataStream
                .Verify(x => x.Delete<AuditedEntity>(ExpectedAuditedEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool RestoreDataWithAuditingWhenDoesNotExistAndRestoredSuccessfully()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(false)
                .Returns(true);
            MockDataStream
                .Setup(x => x.Undelete<AuditedEntity>(ExpectedAuditedEntity.Id))
                .Returns(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedAuditedEntity))
                .Returns(true);
            MockAudit
                .Setup(x => x.CommitEvents(ExpectedAuditedEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedAuditedEntity))
                .Returns(true);
            return DataManager.Restore<AuditedEntity>(ExpectedAuditedEntity.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RestoreDataWhenDoesNotExistButIndexingFails()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(false)
                .Returns(true);
            MockDataStream
                .Setup(x => x.Undelete<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedBasicEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedBasicEntity))
                .Returns(false);
            var result = DataManager.Restore<BasicEntity>(ExpectedBasicEntity.Id);
            MockDataStream
                .Verify(x => x.Delete<BasicEntity>(ExpectedBasicEntity.Id), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool RestoreDataWhenDoesNotExistRestoredSuccessfully()
        {
            MockDataStream
                .SetupSequence(x => x.Exists<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(false)
                .Returns(true);
            MockDataStream
                .Setup(x => x.Undelete<BasicEntity>(ExpectedBasicEntity.Id))
                .Returns(true);
            MockAudit
                .Setup(x => x.UndeleteEvent(ExpectedBasicEntity))
                .Returns(true);
            MockIndexer
                .Setup(x => x.Index(ExpectedBasicEntity))
                .Returns(true);
            var result = DataManager.Restore<BasicEntity>(ExpectedBasicEntity.Id);
            MockDataStream
                .Verify(x => x.Delete<BasicEntity>(ExpectedBasicEntity.Id), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void GetNextIdReturnsDataStreamsNextId()
        {
            Assert.AreEqual(ExpectedNextBasicEntityId, DataManager.GetNextId<BasicEntity>());
        }
    }
}