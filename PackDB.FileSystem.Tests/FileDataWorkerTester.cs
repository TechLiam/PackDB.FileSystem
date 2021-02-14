using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.FileSystem.DataWorker;

namespace PackDB.FileSystem.Tests
{
    [TestFixture]
    [TestOf(typeof(FileDataWorker))]
    [ExcludeFromCodeCoverage]
    public class FileDataWorkerTester
    {
        [SetUp]
        public void Setup()
        {
            Randomizer = Randomizer.CreateRandomizer();
            ExpectedBasicData = new BasicData
            {
                Id = Randomizer.Next()
            };
            ExpectedRetryBasicData = new RetryBasicData
            {
                Id = Randomizer.Next()
            };
            ExpectedSoftDeleteData = new SoftDeleteData
            {
                Id = Randomizer.Next()
            };

            MockFileStreamer = new Mock<IFileStreamer>();
            MockFileStreamer
                .Setup(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<BasicData>("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(ExpectedBasicData);
            MockFileStreamer
                .Setup(x => x.GetLockForFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<RetryBasicData>("RetryBasicData\\" + ExpectedRetryBasicData.Id +
                                                                 ".data"))
                .Returns(ExpectedRetryBasicData);

            FileDataWorker = new FileDataWorker(MockFileStreamer.Object);
        }

        private FileDataWorker FileDataWorker { get; set; }
        private Randomizer Randomizer { get; set; }
        private BasicData ExpectedBasicData { get; set; }
        private RetryBasicData ExpectedRetryBasicData { get; set; }
        private SoftDeleteData ExpectedSoftDeleteData { get; set; }
        private Mock<IFileStreamer> MockFileStreamer { get; set; }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false);
            var result = FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteFailsToGetLockWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false);
            var result = FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteFailsToWriteDataToStream()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .Returns(false);
            var result = FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteFailsToWriteDataToStreamWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<RetryBasicData>()))
                .Returns(false);
            var result = FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteFailsToWriteDataToStreamDueToException()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .Throws<Exception>();
            var result = FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteFailsToWriteDataToStreamDueToExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<RetryBasicData>()))
                .Throws<Exception>();
            var result = FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteSuccessfulFirstTime()
        {
            var result = FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteSuccessfulAfterRetry()
        {
            MockFileStreamer
                .SetupSequence(x => x.WriteDataToStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData))
                .Returns(false)
                .Returns(true);
            var result = FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(2));
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData), Times.Exactly(2));
            MockFileStreamer.Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(1));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CommitFailsToCloseStream()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Returns(false);
            var result = FileDataWorker.Commit<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Once);
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CommitFailsToCloseStreamWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Returns(false);
            var result = FileDataWorker.Commit<RetryBasicData>(ExpectedRetryBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CommitFailsToCloseStreamDueToException()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Throws<Exception>();
            var result = FileDataWorker.Commit<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Once);
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CommitFailsToCloseStreamDueToExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Throws<Exception>();
            var result = FileDataWorker.Commit<RetryBasicData>(ExpectedRetryBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool CommitSuccessful()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Returns(true);
            var result = FileDataWorker.Commit<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            MockFileStreamer
                .Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool CommitSuccessfulWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x => x.CloseStream(It.IsAny<string>()))
                .Returns(false)
                .Returns(true);
            var result = FileDataWorker.Commit<RetryBasicData>(ExpectedRetryBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Exactly(2));
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Never);
            MockFileStreamer
                .Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void DiscardChanges()
        {
            FileDataWorker.DiscardChanges<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"));
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteAndCommitFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false);
            var result = FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteAndCommitFailsToWriteDataToStream()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .Returns(false);
            var result = FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteAndCommitFailsToWriteDataToStreamDueToException()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .Throws<Exception>();
            var result = FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool WriteAndCommitFailsToCloseStream()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Returns(false);
            var result = FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteAndCommitSuccessful()
        {
            var result = FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void ReadFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false);
            Assert.IsNull(FileDataWorker.Read<BasicData>(ExpectedBasicData.Id));
            MockFileStreamer.Verify(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public void ReadFailsDueToReadThrowingAnException()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()))
                .Throws<Exception>();
            Assert.IsNull(FileDataWorker.Read<BasicData>(ExpectedBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public void ReadSuccessful()
        {
            Assert.AreSame(ExpectedBasicData, FileDataWorker.Read<BasicData>(ExpectedBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public void ReadFailsToGetLockWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false);
            Assert.IsNull(FileDataWorker.Read<RetryBasicData>(ExpectedRetryBasicData.Id));
            MockFileStreamer.Verify(x => x.ReadDataFromStream<RetryBasicData>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public void ReadFailsDueToReadThrowingAnExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<RetryBasicData>(It.IsAny<string>()))
                .Throws<Exception>();
            Assert.IsNull(FileDataWorker.Read<RetryBasicData>(ExpectedRetryBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator")]
        public void ReadSuccessfulWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x =>
                    x.ReadDataFromStream<RetryBasicData>("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"))
                .Throws<Exception>()
                .Returns(ExpectedRetryBasicData);
            Assert.AreSame(ExpectedRetryBasicData, FileDataWorker.Read<RetryBasicData>(ExpectedRetryBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool ExistsReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.Exists("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(true);
            return FileDataWorker.Exists<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool ExistsReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.Exists("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(false);
            return FileDataWorker.Exists<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool DeleteHardDeleteReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.Delete("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(true);
            return FileDataWorker.Delete<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteHardDeleteReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.Delete("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .Returns(false);
            return FileDataWorker.Delete<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool DeleteSoftDeleteReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.SoftDelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .Returns(true);
            return FileDataWorker.Delete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool DeleteSoftDeleteReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.SoftDelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .Returns(false);
            return FileDataWorker.Delete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool RestoreSoftDeleteDataReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.Undelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .Returns(true);
            var result = FileDataWorker.Undelete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
            MockFileStreamer.Verify(x => x.Undelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool RestoreSoftDeleteDataReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.Undelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .Returns(false);
            var result = FileDataWorker.Undelete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
            MockFileStreamer.Verify(x => x.Undelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void RollbackWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x => x.GetLockForFile(It.IsAny<string>()))
                .Returns(false)
                .Returns(true);
            FileDataWorker.Rollback(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer
                .Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(2));
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Once);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = 1)]
        public int NextIdWhenThereNoFiles()
        {
            return FileDataWorker.NextId<BasicData>();
        }

        [Test(Author = "PackDB Creator", ExpectedResult = 1)]
        public int NextIdWhenThereAreFilesButThereNumberIsLessThanOne()
        {
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("BasicData", "data"))
                .Returns(new List<string>
                {
                    "0"
                }.ToArray());
            return FileDataWorker.NextId<BasicData>();
        }

        [Test(Author = "PackDB Creator", ExpectedResult = 2)]
        public int NextIdWhenThereAreFilesAndThereNumberIsGraterThanOne()
        {
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("BasicData", "data"))
                .Returns(new List<string>
                {
                    "1"
                }.ToArray());
            return FileDataWorker.NextId<BasicData>();
        }
    }
}