using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
                .Setup(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data",
                    ExpectedBasicData))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.CloseStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<BasicData>("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(ExpectedBasicData);
            MockFileStreamer
                .Setup(x => x.GetLockForFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<RetryBasicData>("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id +
                                                                 ".data"))
                .ReturnsAsync(ExpectedRetryBasicData);
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("Data\\BasicData", "data"))
                .Returns(new List<string>
                {
                    ExpectedBasicData.Id.ToString()
                }.ToArray());
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("Data\\RetryBasicData", "data"))
                .Returns(new List<string>
                {
                    ExpectedRetryBasicData.Id.ToString()
                }.ToArray());

            FileDataWorker = new FileDataWorker(MockFileStreamer.Object);
        }

        private FileDataWorker FileDataWorker { get; set; }
        private Randomizer Randomizer { get; set; }
        private BasicData ExpectedBasicData { get; set; }
        private RetryBasicData ExpectedRetryBasicData { get; set; }
        private SoftDeleteData ExpectedSoftDeleteData { get; set; }
        private Mock<IFileStreamer> MockFileStreamer { get; set; }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteFailsToGetLockWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteFailsToWriteDataToStream()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteFailsToWriteDataToStreamWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<RetryBasicData>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(
                x => x.GetLockForFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteFailsToWriteDataToStreamDueToException()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .Throws<Exception>();
            var result = await FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteFailsToWriteDataToStreamDueToExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<RetryBasicData>()))
                .Throws<Exception>();
            var result = await FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(
                x => x.GetLockForFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData), Times.Exactly(3));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteSuccessfulFirstTime()
        {
            var result = await FileDataWorker.Write(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteSuccessfulAfterRetry()
        {
            MockFileStreamer
                .SetupSequence(x => x.WriteDataToStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            var result = await FileDataWorker.Write(ExpectedRetryBasicData.Id, ExpectedRetryBasicData);
            MockFileStreamer.Verify(
                x => x.GetLockForFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(2));
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data",
                    ExpectedRetryBasicData), Times.Exactly(2));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(1));
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CommitFailsToCloseStream()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Commit<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Once);
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CommitFailsToCloseStreamWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Commit<RetryBasicData>(ExpectedRetryBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                    Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CommitFailsToCloseStreamDueToException()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Throws<Exception>();
            var result = await FileDataWorker.Commit<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Once);
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CommitFailsToCloseStreamDueToExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Throws<Exception>();
            var result = await FileDataWorker.Commit<RetryBasicData>(ExpectedRetryBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Exactly(3));
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                    Times.Once);
            MockFileStreamer
                .Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> CommitSuccessful()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(true);
            var result = await FileDataWorker.Commit<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            MockFileStreamer
                .Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> CommitSuccessfulWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            var result = await FileDataWorker.Commit<RetryBasicData>(ExpectedRetryBasicData.Id);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                    Times.Exactly(2));
            MockFileStreamer
                .Verify(x => x.DisposeOfStream("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                    Times.Never);
            MockFileStreamer
                .Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task DiscardChanges()
        {
            await FileDataWorker.DiscardChanges<BasicData>(ExpectedBasicData.Id);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteAndCommitFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("BasicData\\" + ExpectedBasicData.Id + ".data"), Times.Never);
            MockFileStreamer.Verify(x => x.DisposeOfStream("BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteAndCommitFailsToWriteDataToStream()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteAndCommitFailsToWriteDataToStreamDueToException()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<BasicData>()))
                .Throws<Exception>();
            var result = await FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> WriteAndCommitFailsToCloseStream()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteAndCommitSuccessful()
        {
            var result = await FileDataWorker.WriteAndCommit(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer.Verify(x => x.GetLockForFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(
                x => x.WriteDataToStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data", ExpectedBasicData),
                Times.Once);
            MockFileStreamer.Verify(x => x.CloseStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            MockFileStreamer.Verify(x => x.DisposeOfStream("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Never);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            Assert.IsNull(await FileDataWorker.Read<BasicData>(ExpectedBasicData.Id));
            MockFileStreamer.Verify(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadFailsDueToReadThrowingAnException()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()))
                .Throws<Exception>();
            Assert.IsNull(await FileDataWorker.Read<BasicData>(ExpectedBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadSuccessful()
        {
            Assert.AreSame(ExpectedBasicData, await FileDataWorker.Read<BasicData>(ExpectedBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadFailsToGetLockWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            Assert.IsNull(await FileDataWorker.Read<RetryBasicData>(ExpectedRetryBasicData.Id));
            MockFileStreamer.Verify(x => x.ReadDataFromStream<RetryBasicData>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadFailsDueToReadThrowingAnExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<RetryBasicData>(It.IsAny<string>()))
                .Throws<Exception>();
            Assert.IsNull(await FileDataWorker.Read<RetryBasicData>(ExpectedRetryBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadSuccessfulWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x =>
                    x.ReadDataFromStream<RetryBasicData>("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id +
                                                         ".data"))
                .Throws<Exception>()
                .ReturnsAsync(ExpectedRetryBasicData);
            Assert.AreSame(ExpectedRetryBasicData,
                await FileDataWorker.Read<RetryBasicData>(ExpectedRetryBasicData.Id));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllWhenThereAreNoFiles()
        {
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("Data\\BasicData", "data"))
                .Returns(new List<string>().ToArray());
            var dataSet = FileDataWorker.ReadAll<BasicData>();
            var results = new List<BasicData>();
            await foreach(var data in dataSet) results.Add(data);
            Assert.IsEmpty(results);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()), Times.Never);
        }
        
        [Test(Author = "PackDB Creator")]
        public async Task ReadAllFailsToGetLock()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            var dataSet = FileDataWorker.ReadAll<BasicData>();
            var results = new List<BasicData>();
            await foreach(var data in dataSet) results.Add(data);
            Assert.IsEmpty(results);
            MockFileStreamer.Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Once);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllFailsDueToReadThrowingAnException()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<BasicData>(It.IsAny<string>()))
                .Throws<Exception>();
            var dataSet = FileDataWorker.ReadAll<BasicData>();
            var results = new List<BasicData>();
            await foreach(var data in dataSet) results.Add(data);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllSuccessful()
        {
            var dataSet = FileDataWorker.ReadAll<BasicData>();
            var results = new List<BasicData>();
            await foreach(var data in dataSet) results.Add(data);
            Assert.AreSame(ExpectedBasicData, results.ElementAt(0));
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"),
                Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllFailsToGetLockWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false);
            var dataSet = FileDataWorker.ReadAll<BasicData>();
            var results = new List<BasicData>();
            await foreach(var data in dataSet) results.Add(data);
            MockFileStreamer.Verify(x => x.ReadDataFromStream<RetryBasicData>(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllFailsDueToReadThrowingAnExceptionWithRetry()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<RetryBasicData>(It.IsAny<string>()))
                .Throws<Exception>();
            var dataSet = FileDataWorker.ReadAll<RetryBasicData>();
            var results = new List<RetryBasicData>();
            await foreach(var data in dataSet) results.Add(data);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(3));
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadAllSuccessfulWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x =>
                    x.ReadDataFromStream<RetryBasicData>("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"))
                .Throws<Exception>()
                .ReturnsAsync(ExpectedRetryBasicData);
            var dataSet = FileDataWorker.ReadAll<RetryBasicData>();
            var results = new List<RetryBasicData>();
            await foreach(var data in dataSet) results.Add(data);
            MockFileStreamer.Verify(x => x.UnlockFile("Data\\RetryBasicData\\" + ExpectedRetryBasicData.Id + ".data"),
                Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> ExistsReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(true);
            return await FileDataWorker.Exists<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> ExistsReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.Exists("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(false);
            return await FileDataWorker.Exists<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> DeleteHardDeleteReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.Delete("Data\\BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(true);
            return await FileDataWorker.Delete<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteHardDeleteReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.Delete("BasicData\\" + ExpectedBasicData.Id + ".data"))
                .ReturnsAsync(false);
            return await FileDataWorker.Delete<BasicData>(ExpectedBasicData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> DeleteSoftDeleteReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.SoftDelete("Data\\SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .ReturnsAsync(true);
            return await FileDataWorker.Delete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> DeleteSoftDeleteReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.SoftDelete("SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .ReturnsAsync(false);
            return await FileDataWorker.Delete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> RestoreSoftDeleteDataReturnsTrue()
        {
            MockFileStreamer
                .Setup(x => x.Undelete("Data\\SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .ReturnsAsync(true);
            var result = await FileDataWorker.Undelete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
            MockFileStreamer.Verify(x => x.Undelete("Data\\SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> RestoreSoftDeleteDataReturnsFalse()
        {
            MockFileStreamer
                .Setup(x => x.Undelete("Data\\SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"))
                .ReturnsAsync(false);
            var result = await FileDataWorker.Undelete<SoftDeleteData>(ExpectedSoftDeleteData.Id);
            MockFileStreamer.Verify(x => x.Undelete("Data\\SoftDeleteData\\" + ExpectedSoftDeleteData.Id + ".data"),
                Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task RollbackWithRetry()
        {
            MockFileStreamer
                .SetupSequence(x => x.GetLockForFile(It.IsAny<string>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            await FileDataWorker.Rollback(ExpectedBasicData.Id, ExpectedBasicData);
            MockFileStreamer
                .Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Exactly(2));
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task RollbackWithRetryForSoftDelete()
        {
            MockFileStreamer
                .SetupSequence(x => x.Undelete(It.IsAny<string>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            await FileDataWorker.Rollback(ExpectedSoftDeleteData.Id, ExpectedSoftDeleteData);
            MockFileStreamer
                .Verify(x => x.Undelete(It.IsAny<string>()), Times.Exactly(2));
            MockFileStreamer
                .Verify(x => x.GetLockForFile(It.IsAny<string>()), Times.Never);
            MockFileStreamer
                .Verify(x => x.CloseStream(It.IsAny<string>()), Times.Never);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = 1)]
        public int NextIdWhenThereNoFiles()
        {
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("Data\\BasicData", "data"))
                .Returns(new List<string>().ToArray());
            return FileDataWorker.NextId<BasicData>();
        }

        [Test(Author = "PackDB Creator", ExpectedResult = 1)]
        public int NextIdWhenThereAreFilesButThereNumberIsLessThanOne()
        {
            MockFileStreamer
                .Setup(x => x.GetAllFileNames("Data\\BasicData", "data"))
                .Returns(new List<string>
                {
                    "0"
                }.ToArray());
            return FileDataWorker.NextId<BasicData>();
        }

        [Test(Author = "PackDB Creator")]
        public void NextIdWhenThereAreFilesAndThereNumberIsGraterThanOne()
        {
            var result = FileDataWorker.NextId<BasicData>();
            Assert.AreEqual(ExpectedBasicData.Id + 1, result);
        }
    }
}