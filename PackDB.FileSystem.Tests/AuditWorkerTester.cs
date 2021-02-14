using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
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
                .Setup(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<AuditLog>("AuditableData\\" + ExpectedData.Id + ".audit"))
                .Returns(CreateExpectedAuditLog);
            MockFileStreamer
                .Setup(x => x.CloseStream("AuditableData\\" + ExpectedData.Id + ".audit"))
                .Returns(true);
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

        private bool GetLockForFileFails(Expression<Func<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, Times retryAttempts)
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"))
                .Returns(false);
            var result = methodUnderTest.Compile().Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"),
                retryAttempts);
            MockFileStreamer.Verify(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<AuditableData>()),
                Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Never);
            MockAuditGenerator.Verify(auditGenerator, Times.Never);
            return result;
        }

        private bool WriteDataToStreamFails(Expression<Func<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, AuditLog expectedAuditLog, Times retryAttempts,
            Times lockFile)
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog))
                .Returns(false);
            var result = methodUnderTest.Compile().Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"), lockFile);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog),
                retryAttempts);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), lockFile);
            MockAuditGenerator.Verify(auditGenerator, retryAttempts);
            return result;
        }

        private bool WriteDataToStreamFailsDueToException(Expression<Func<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, AuditLog expectedAuditLog, Times retryAttempts,
            Times lockFile)
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog))
                .Throws<Exception>();
            var result = methodUnderTest.Compile().Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"), lockFile);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog),
                retryAttempts);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), lockFile);
            MockAuditGenerator.Verify(auditGenerator, retryAttempts);
            return result;
        }

        private bool GenerateAuditFailsDueToException(Expression<Func<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, Times retryAttempts)
        {
            MockAuditGenerator
                .Setup(auditGenerator)
                .Throws<Exception>();
            var result = methodUnderTest.Compile().Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"),
                retryAttempts);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", It.IsAny<AuditLog>()),
                Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile("AuditableData\\" + ExpectedData.Id + ".audit"), retryAttempts);
            MockAuditGenerator.Verify(auditGenerator, Times.Exactly(3));
            return result;
        }

        private bool SuccessEvent(Expression<Func<bool>> methodUnderTest,
            Expression<Func<IAuditGenerator, AuditLog>> auditGenerator, AuditLog expectedAuditLog,
            Times getFileLockTimes, Times unlockFile)
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog))
                .Returns(true);
            var result = methodUnderTest.Compile().Invoke();
            MockFileStreamer.Verify(x => x.GetLockForFile("AuditableData\\" + ExpectedData.Id + ".audit"),
                getFileLockTimes);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("AuditableData\\" + ExpectedData.Id + ".audit", expectedAuditLog), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), unlockFile);
            MockAuditGenerator.Verify(auditGenerator, Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CreationEventGetLockForFileFails()
        {
            return GetLockForFileFails(() => FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CreationEventWriteDataToStreamFails()
        {
            return WriteDataToStreamFails(() => FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()),
                CreateExpectedAuditLog, Times.Exactly(3), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CreationEventWriteDataToStreamFailsDueToException()
        {
            return WriteDataToStreamFailsDueToException(() => FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()),
                CreateExpectedAuditLog, Times.Exactly(3), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CreationEventGenerateAuditFailsDueToException()
        {
            return GenerateAuditFailsDueToException(() => FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(It.IsAny<AuditableData>()), Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool CreationEventSuccess()
        {
            return SuccessEvent(() => FileAuditWorker.CreationEvent(ExpectedData),
                x => x.NewLog(ExpectedData),
                CreateExpectedAuditLog, Times.Once(), Times.Never());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UpdateEventGetLockForFileFails()
        {
            return GetLockForFileFails(() => FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UpdateEventWriteDataToStreamFails()
        {
            return WriteDataToStreamFails(() => FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UpdateExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UpdateEventWriteDataToStreamFailsDueToException()
        {
            return WriteDataToStreamFailsDueToException(
                () => FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UpdateExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UpdateEventGenerateAuditFailsDueToException()
        {
            return GenerateAuditFailsDueToException(() => FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(It.IsAny<AuditableData>(), It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool UpdateEventSuccess()
        {
            return SuccessEvent(() => FileAuditWorker.UpdateEvent(ExpectedData, OldExpectedData),
                x => x.UpdateLog(ExpectedData, OldExpectedData, CreateExpectedAuditLog),
                UpdateExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteEventGetLockForFileFails()
        {
            return GetLockForFileFails(() => FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteEventWriteDataToStreamFails()
        {
            return WriteDataToStreamFails(() => FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                DeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteEventWriteDataToStreamFailsDueToException()
        {
            return WriteDataToStreamFailsDueToException(() => FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                DeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteEventGenerateAuditFailsDueToException()
        {
            return GenerateAuditFailsDueToException(() => FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool DeleteEventSuccess()
        {
            return SuccessEvent(() => FileAuditWorker.DeleteEvent(ExpectedData),
                x => x.DeleteLog(ExpectedData, CreateExpectedAuditLog),
                DeleteExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UndeleteEventGetLockForFileFails()
        {
            return GetLockForFileFails(() => FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UndeleteEventWriteDataToStreamFails()
        {
            return WriteDataToStreamFails(() => FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UndeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UndeleteEventWriteDataToStreamFailsDueToException()
        {
            return WriteDataToStreamFailsDueToException(() => FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                UndeleteExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool UndeleteEventGenerateAuditFailsDueToException()
        {
            return GenerateAuditFailsDueToException(() => FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool UndeleteEventSuccess()
        {
            return SuccessEvent(() => FileAuditWorker.UndeleteEvent(ExpectedData),
                x => x.UndeleteLog(ExpectedData, CreateExpectedAuditLog),
                UndeleteExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RollbackEventGetLockForFileFails()
        {
            return GetLockForFileFails(() => FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(6));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RollbackEventWriteDataToStreamFails()
        {
            return WriteDataToStreamFails(() => FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                RollbackExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RollbackEventWriteDataToStreamFailsDueToException()
        {
            return WriteDataToStreamFailsDueToException(() => FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()),
                RollbackExpectedAuditLog, Times.Exactly(3), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RollbackEventGenerateAuditFailsDueToException()
        {
            return GenerateAuditFailsDueToException(() => FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(It.IsAny<AuditableData>(), It.IsAny<AuditLog>()), Times.Exactly(4));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool RollbackEventSuccess()
        {
            return SuccessEvent(() => FileAuditWorker.RollbackEvent(ExpectedData),
                x => x.RollbackLog(ExpectedData, CreateExpectedAuditLog),
                RollbackExpectedAuditLog, Times.Exactly(2), Times.Once());
        }

        [Test(Author = "PackDB Creator")]
        public void DiscardChanges()
        {
            FileAuditWorker.DiscardEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream("AuditableData\\" + ExpectedData.Id + ".audit"));
            MockFileStreamer.Verify(x => x.UnlockFile("AuditableData\\" + ExpectedData.Id + ".audit"));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CommitEventsCloseStreamFails()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Returns(false);
            var result = FileAuditWorker.CommitEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream("AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CommitEventsCloseStreamFailsDueToException()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Throws<Exception>();
            var result = FileAuditWorker.CommitEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream("AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool CommitEventsSuccess()
        {
            var result = FileAuditWorker.CommitEvents(ExpectedData);
            MockFileStreamer.Verify(x => x.DisposeOfStream(It.IsAny<string>()), Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile("AuditableData\\" + ExpectedData.Id + ".audit"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = null)]
        public AuditLog ReadAllEventsGetLockForFileFails()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false);
            var result = FileAuditWorker.ReadAllEvents<AuditableData>(ExpectedData.Id);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Never);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = null)]
        public AuditLog ReadAllEventsReadDataFromStreamFailsDueToException()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()))
                .Throws<Exception>();
            var result = FileAuditWorker.ReadAllEvents<AuditableData>(ExpectedData.Id);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()), Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void ReadAllEventsSuccessful()
        {
            var result = FileAuditWorker.ReadAllEvents<AuditableData>(ExpectedData.Id);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<AuditLog>(It.IsAny<string>()), Times.Once);
            Assert.AreSame(CreateExpectedAuditLog, result);
        }
    }
}