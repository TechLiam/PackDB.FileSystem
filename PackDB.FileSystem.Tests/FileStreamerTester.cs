using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Locks;
using PackDB.Core.MessagePackProxy;
using PackDB.FileSystem.OS;

namespace PackDB.FileSystem.Tests
{
    [TestFixture]
    [TestOf(typeof(FileStreamer))]
    [ExcludeFromCodeCoverage]
    public class FileStreamerTester
    {
        [SetUp]
        public void Setup()
        {
            Filename = Randomizer.CreateRandomizer().GetString();
            Data = new BasicData();
            WriteStream = new MemoryStream();
            ReadStream = new MemoryStream();

            MockWriteStream = new Mock<IStream>();
            MockWriteStream
                .Setup(x => x.GetStream())
                .Returns(WriteStream);
            MockReadStream = new Mock<IStream>();
            MockReadStream
                .Setup(x => x.GetStream())
                .Returns(ReadStream);

            MockMessagePackSerializer = new Mock<IMessagePackSerializer>();
            MockMessagePackSerializer
                .Setup(x => x.Deserialize<BasicData>(ReadStream))
                .ReturnsAsync(Data);

            MockFile = new Mock<IFile>();
            MockFile
                .Setup(x => x.OpenWrite(Filename))
                .ReturnsAsync(MockWriteStream.Object);
            MockFile
                .Setup(x => x.OpenRead(Filename))
                .ReturnsAsync(MockReadStream.Object);
            MockFile
                .Setup(x => x.Exists(Filename))
                .ReturnsAsync(true);

            MockSemaphore = new Mock<ISemaphore>();
            MockSemaphore
                .Setup(x => x.Wait(It.Is<TimeSpan>(y => Math.Abs(y.TotalMinutes - 1.0) < 1)))
                .Returns(true);

            MockSemaphoreFactory = new Mock<ISemaphoreFactory>();
            MockSemaphoreFactory
                .Setup(x => x.Create(1, 1))
                .Returns(MockSemaphore.Object);

            MockDirectory = new Mock<IDirectory>();

            FileStreamer = new FileStreamer(MockMessagePackSerializer.Object, MockFile.Object,
                MockSemaphoreFactory.Object, MockDirectory.Object);
        }

        private FileStreamer FileStreamer { get; set; }
        private string Filename { get; set; }
        private BasicData Data { get; set; }
        private Stream WriteStream { get; set; }
        private Stream ReadStream { get; set; }
        private Mock<IStream> MockWriteStream { get; set; }
        private Mock<IStream> MockReadStream { get; set; }
        private Mock<IMessagePackSerializer> MockMessagePackSerializer { get; set; }
        private Mock<IFile> MockFile { get; set; }
        private Mock<IDirectory> MockDirectory { get; set; }
        private Mock<ISemaphoreFactory> MockSemaphoreFactory { get; set; }
        private Mock<ISemaphore> MockSemaphore { get; set; }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> GetLockForFileWaitFails()
        {
            MockSemaphore
                .Setup(x => x.Wait(It.Is<TimeSpan>(y => Math.Abs(y.TotalMinutes - 1) < 1)))
                .Returns(false);
            var result = FileStreamer.GetLockForFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1, 1), Times.Once);
            return await result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> GetLockForFileWaitSuccess()
        {
            var result = FileStreamer.GetLockForFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1, 1), Times.Once);
            return await result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> GetLockForFileCreateOnlyOneSemaphore()
        {
            await FileStreamer.GetLockForFile(Filename);
            var result = FileStreamer.GetLockForFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1, 1), Times.Once);
            return await result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task UnlockFileWhenThereIsNoLock()
        {
            await FileStreamer.UnlockFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1, 1), Times.Never);
            MockSemaphore.Verify(x => x.Release(), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task UnlockFileWhenThereIsALock()
        {
            await FileStreamer.GetLockForFile(Filename);
            await FileStreamer.UnlockFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1, 1), Times.Once);
            MockSemaphore.Verify(x => x.Release(), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task UnlockFileWhenThereIsALockMultipleTimes()
        {
            await FileStreamer.GetLockForFile(Filename);
            await FileStreamer.UnlockFile(Filename);
            await FileStreamer.UnlockFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1, 1), Times.Once);
            MockSemaphore.Verify(x => x.Release(), Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteDataToStreamNewStreamNeeded()
        {
            var result = FileStreamer.WriteDataToStream(Filename, Data);
            MockFile.Verify(x => x.OpenWrite(Filename), Times.Once);
            MockMessagePackSerializer.Verify(x => x.Serialize(WriteStream, Data), Times.Once);
            return await result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> WriteDataToStreamMultipleTimes()
        {
            await FileStreamer.WriteDataToStream(Filename, Data);
            var result = FileStreamer.WriteDataToStream(Filename, Data);
            MockFile.Verify(x => x.OpenWrite(Filename), Times.Once);
            MockMessagePackSerializer.Verify(x => x.Serialize(WriteStream, Data), Times.Exactly(2));
            return await result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadDataFromStreamNewStreamNeeded()
        {
            Assert.AreSame(Data, await FileStreamer.ReadDataFromStream<BasicData>(Filename));
            MockFile.Verify(x => x.OpenRead(Filename), Times.Once);
            MockReadStream.Verify(x => x.Close(), Times.Once);
            MockReadStream.Verify(x => x.Dispose(), Times.Once);
            MockMessagePackSerializer.Verify(x => x.Deserialize<BasicData>(ReadStream), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task ReadDataFromStreamMultipleTimes()
        {
            await FileStreamer.ReadDataFromStream<BasicData>(Filename);
            Assert.AreSame(Data, await FileStreamer.ReadDataFromStream<BasicData>(Filename));
            MockFile.Verify(x => x.OpenRead(Filename), Times.Exactly(2));
            MockMessagePackSerializer.Verify(x => x.Deserialize<BasicData>(ReadStream), Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CloseStreamWhenThereIsNoStream()
        {
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            return await result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> CloseStreamWhenThereIsAWriteStream()
        {
            await FileStreamer.WriteDataToStream(Filename, Data);
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Once);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Once);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
            return await result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CloseStreamWhenThereIsAWriteStreamIsAlreadyClosed()
        {
            await FileStreamer.WriteDataToStream(Filename, Data);
            await FileStreamer.CloseStream(Filename);
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Once);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Once);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
            return await result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> CloseStreamWhenThereIsAReadStreamIsAlreadyClosed()
        {
            await FileStreamer.ReadDataFromStream<BasicData>(Filename);
            await FileStreamer.CloseStream(Filename);
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Never);
            MockReadStream
                .Verify(x => x.Close(), Times.Once);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Once);
            return await result;
        }

        [Test(Author = "PackDB Creator")]
        public async Task DisposeOfStreamWhenThereIsNoStream()
        {
            await FileStreamer.DisposeOfStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task DisposeOfStreamWhenThereIsAWriteStream()
        {
            await FileStreamer.WriteDataToStream(Filename, Data);
            await FileStreamer.DisposeOfStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Once);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task DisposeOfStreamWhenThereIsAReadStream()
        {
            await FileStreamer.ReadDataFromStream<BasicData>(Filename);
            await FileStreamer.DisposeOfStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Never);
            MockReadStream
                .Verify(x => x.Close(), Times.Once);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task DisposeOfStreamWhenThereIsAWriteStreamIsAlreadyDisposed()
        {
            await FileStreamer.WriteDataToStream(Filename, Data);
            await FileStreamer.DisposeOfStream(Filename);
            await FileStreamer.DisposeOfStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Once);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public async Task DisposeOfStreamWhenThereIsAReadStreamIsAlreadyDisposed()
        {
            await FileStreamer.ReadDataFromStream<BasicData>(Filename);
            await FileStreamer.DisposeOfStream(Filename);
            await FileStreamer.DisposeOfStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Never);
            MockReadStream
                .Verify(x => x.Close(), Times.Once);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Once);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> ExistsWhenItDoesNotExist()
        {
            MockFile
                .Setup(x => x.Exists(Filename))
                .ReturnsAsync(false);
            return await FileStreamer.Exists(Filename);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> ExistsWhenItDoesExist()
        {
            return await FileStreamer.Exists(Filename);
        }

        [Test(Author = "PackDB Creator")]
        public async Task Delete()
        {
            await FileStreamer.Delete(Filename);
            MockFile.Verify(x => x.Delete(Filename), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task SoftDelete()
        {
            await FileStreamer.SoftDelete(Filename);
            MockFile.Verify(x => x.Move(Filename, Filename + ".deleted"), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task UndeleteWhenFileExists()
        {
            MockFile.Setup(x => x.Exists(Filename + ".deleted")).ReturnsAsync(true);
            await FileStreamer.Undelete(Filename);
            MockFile.Verify(x => x.Move(Filename + ".deleted", Filename), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public async Task UndeleteWhenFileDoesNotExists()
        {
            await FileStreamer.Undelete(Filename);
            MockFile.Verify(x => x.Move(Filename + ".deleted", Filename), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public void GetAllFileNamesWhenThereNoFiles()
        {
            var results = FileStreamer.GetAllFileNames("Test", "Test");
            Assert.IsNotNull(results);
            Assert.IsEmpty(results);
        }

        [Test(Author = "PackDB Creator")]
        public void GetAllFileNamesWhenAreFiles()
        {
            MockDirectory
                .Setup(x => x.GetFiles("Test", "Test"))
                .Returns(new List<string>
                {
                    "Test1",
                    "Test\\Test2",
                    "Test\\Test3.Test"
                }.ToArray());
            var results = FileStreamer.GetAllFileNames("Test", "Test");
            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results);
            Assert.AreEqual(3, results.Count());
            Assert.AreEqual("Test1", results[0]);
            Assert.AreEqual("Test2", results[1]);
            Assert.AreEqual("Test3", results[2]);
        }
    }
}