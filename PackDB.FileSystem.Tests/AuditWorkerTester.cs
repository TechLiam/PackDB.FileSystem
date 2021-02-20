using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Auditing;
using PackDB.FileSystem.AuditWorker;

namespace PackDB.FileSystem.Tests
{
    [TestFixture]
    [TestOf(typeof(FileAuditWorker))]
    [ExcludeFromCodeCoverage]
    public class AuditWorkerTester
    {
        [SetUp]
        public void Setup()
        {
            Randomizer = Randomizer.CreateRandomizer();

            ExpectedData = new AuditableData
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString(),
                Value2 = Randomizer.Next()
            };
            OldExpectedData = new AuditableData
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString(),
                Value2 = Randomizer.Next()
            };
            CreateExpectedAuditLog = new AuditLog();
            UpdateExpectedAuditLog = new AuditLog();
            DeleteExpectedAuditLog = new AuditLog();
            UndeleteExpectedAuditLog = new AuditLog();
            RollbackExpectedAuditLog = new AuditLog();

            MockFileStreamer = new Mock<IFileStreamer>();
            MockFileStreamer
                .Setup(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<AuditLog>("Data\\AuditableData\\" + ExpectedData.Id + ".audit"))
                .ReturnsAsync(CreateExpectedAuditLog);
            MockFileStreamer
                .Setup(x => x.CloseStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit"))
                .ReturnsAsync(true);
            MockAuditGenerator = new Mock<IAuditGenerator>();
            MockAuditGenerator
                .Setup(x => x.NewLog(ExpectedData))
                .Returns(CreateExpectedAuditLog);
            MockAuditGenerator
                .Setup(x => x.UpdateLog(ExpectedData, OldExpectedData, CreateExpectedAuditLog))
                .Returns(UpdateExpectedAuditLog);
            MockAuditGenerator
                .Setup(x => x.DeleteLog(ExpectedData, CreateExpectedAuditLog))
                .Returns(DeleteExpectedAuditLog);
            MockAuditGenerator
                .Setup(x => x.UndeleteLog(ExpectedData, CreateExpectedAuditLog))
                .Returns(UndeleteExpectedAuditLog);
            MockAuditGenerator
                .Setup(x => x.RollbackLog(ExpectedData, CreateExpectedAuditLog))
                .Returns(RollbackExpectedAuditLog);
            FileAuditWorker = new FileAuditWorker(MockFileStreamer.Object, MockAuditGenerator.Object);
        }

        private FileAuditWorker FileAuditWorker { get; set; }
        private Randomizer Randomizer { get; set; }
        private AuditableData ExpectedData { get; set; }
        private AuditableData OldExpectedData { get; set; }
        private AuditLog CreateExpectedAuditLog { get; set; }
        private AuditLog UpdateExpectedAuditLog { get; set; }
        private AuditLog DeleteExpectedAuditLog { get; set; }
        private AuditLog UndeleteExpectedAuditLog { get; set; }
        private AuditLog RollbackExpectedAuditLog { get; set; }
        private Mock<IFileStreamer> MockFileStreamer { get; set; }
        private Mock<IAuditGenerator> MockAuditGenerator { get; set; }

        private async Task<bool> GetLockForFileFails(Func<Task<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, Times retryAttempts)
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"))
                .ReturnsAsync(false);
            var result = await methodUnderTest.Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"),
                retryAttempts);
            MockFileStreamer.Verify(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<AuditableData>()),
                Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Never);
            MockAuditGenerator.Verify(auditGenerator, Times.Never);
            return result;
        }

        private async Task<bool> WriteDataToStreamFails(Func<Task<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, AuditLog expectedAuditLog, Times retryAttempts,
            Times lockFile)
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog))
                .ReturnsAsync(false);
            var result = await methodUnderTest.Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), lockFile);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog),
                retryAttempts);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), lockFile);
            MockAuditGenerator.Verify(auditGenerator, retryAttempts);
            return result;
        }

        private async Task<bool> WriteDataToStreamFailsDueToException(Func<Task<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, AuditLog expectedAuditLog, Times retryAttempts,
            Times lockFile)
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog))
                .Throws<Exception>();
            var result = await methodUnderTest.Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), lockFile);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog),
                retryAttempts);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), lockFile);
            MockAuditGenerator.Verify(auditGenerator, retryAttempts);
            return result;
        }

        private async Task<bool> GenerateAuditFailsDueToException(Func<Task<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, Times retryAttempts)
        {
            MockAuditGenerator
                .Setup(auditGenerator)
                .Throws<Exception>();
            var result = await methodUnderTest.Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"),
                retryAttempts);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", It.IsAny<AuditLog>()),
                Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), retryAttempts);
            MockAuditGenerator.Verify(auditGenerator, Times.Exactly(3));
            return result;
        }

        private async Task<bool> SuccessEvent(Func<Task<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, AuditLog expectedAuditLog,
            Times getFileLockTimes, Times unlockFile)
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog))
                .ReturnsAsync(true);
            var result = await methodUnderTest.Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"),
                getFileLockTimes);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), unlockFile);
            MockAuditGenerator.Verify(auditGenerator, Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CreationEventGetLockForFileFails()
        {
            return await GetLockForFileFails(async () => await FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CreationEventWriteDataToStreamFails()
        {
            return await WriteDataToStreamFails(async () => await FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()),
                CreateExpectedAuditLog, Times.Exactly(3), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CreationEventWriteDataToStreamFailsDueToException()
        {
            return await WriteDataToStreamFailsDueToException(async () => await FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()),
                CreateExpectedAuditLog, Times.Exactly(3), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CreationEventGenerateAuditFailsDueToException()
        {
            return await GenerateAuditFailsDueToException(async () => await FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> CreationEventSuccess()
        {
            return await SuccessEvent(async () => await FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(ExpectedData),
                CreateExpectedAuditLog, Times.Once(), Times.Never());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UpdateEventGetLockForFileFails()
        {
            return await GetLockForFileFails(async () => await FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UpdateEventWriteDataToStreamFails()
        {
            return await WriteDataToStreamFails(async () => await FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UpdateExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UpdateEventWriteDataToStreamFailsDueToException()
        {
            return await WriteDataToStreamFailsDueToException(
                async () => await FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UpdateExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UpdateEventGenerateAuditFailsDueToException()
        {
            return await GenerateAuditFailsDueToException(async () => await FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UpdateEventSuccess()
        {
            return await SuccessEvent(async () => await FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(ExpectedData, OldExpectedData, CreateExpectedAuditLog),
                UpdateExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteEventGetLockForFileFails()
        {
            return await GetLockForFileFails(async () => await FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteEventWriteDataToStreamFails()
        {
            return await WriteDataToStreamFails(async () => await FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                DeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteEventWriteDataToStreamFailsDueToException()
        {
            return await WriteDataToStreamFailsDueToException(async () => await FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                DeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteEventGenerateAuditFailsDueToException()
        {
            return await GenerateAuditFailsDueToException(async () => await FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> DeleteEventSuccess()
        {
            return await SuccessEvent(async () => await FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(ExpectedData, CreateExpectedAuditLog),
                DeleteExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UndeleteEventGetLockForFileFails()
        {
            return await GetLockForFileFails(async () => await FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UndeleteEventWriteDataToStreamFails()
        {
            return await WriteDataToStreamFails(async () => await FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UndeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UndeleteEventWriteDataToStreamFailsDueToException()
        {
            return await WriteDataToStreamFailsDueToException(async () => await FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UndeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UndeleteEventGenerateAuditFailsDueToException()
        {
            return await GenerateAuditFailsDueToException(async () => await FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UndeleteEventSuccess()
        {
            return await SuccessEvent(async () => await FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(ExpectedData, CreateExpectedAuditLog),
                UndeleteExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RollbackEventGetLockForFileFails()
        {
            return await GetLockForFileFails(async () => await FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RollbackEventWriteDataToStreamFails()
        {
            return await WriteDataToStreamFails(async () => await FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                RollbackExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RollbackEventWriteDataToStreamFailsDueToException()
        {
            return await WriteDataToStreamFailsDueToException(async () => await FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                RollbackExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RollbackEventGenerateAuditFailsDueToException()
        {
            return await GenerateAuditFailsDueToException(async () => await FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> RollbackEventSuccess()
        {
            return await SuccessEvent(async () => await FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(ExpectedData, CreateExpectedAuditLog),
                RollbackExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator")]
        public async Task DiscardChanges()
        {
            await FileAuditWorker.DiscardEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit"));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CommitEventsCloseStreamFails()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileAuditWorker.CommitEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CommitEventsCloseStreamFailsDueToException()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Throws<Exception>();
            var result = await FileAuditWorker.CommitEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> CommitEventsSuccess()
        {
            var result = await FileAuditWorker.CommitEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream(It.IsAny<string>()), Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = null)]
        public async Task<AuditLog> ReadAllEventsGetLockForFileFails()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileAuditWorker.ReadAllEvents<AuditableData>(ExpectedData.Id);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Never);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = null)]
        public async Task<AuditLog> ReadAllEventsReadDataFromStreamFailsDueToException()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()))
                .Throws<Exception>();
            var result = await FileAuditWorker.ReadAllEvents<AuditableData>(ExpectedData.Id);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()), Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllEventsSuccessful()
        {
            var result = await FileAuditWorker.ReadAllEvents<AuditableData>(ExpectedData.Id);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()), Times.Once);
            Assert.AreSame(CreateExpectedAuditLog, result);
        }
    }
}