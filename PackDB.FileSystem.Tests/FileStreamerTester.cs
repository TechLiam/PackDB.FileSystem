using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Locks;
using PackDB.Core.MessagePackProxy;
using PackDB.FileSystem.OS;

namespace PackDB.FileSystem.Tests
{
    
    [TestFixture,TestOf(typeof(FileStreamer)),ExcludeFromCodeCoverage]
    public class FileStreamerTester
    {
        
        private FileStreamer FileStreamer { get; set; }
        private string Filename { get; set; }
        private BasicData Data { get; set; }
        private Stream WriteStream { get; set; }
        private Stream ReadStream { get; set; }
        private Mock<IStream> MockWriteStream { get; set; }
        private Mock<IStream> MockReadStream { get; set; }
        private Mock<IMessagePackSerializer> MockMessagePackSerializer { get; set; }
        private Mock<IFile> MockFile { get; set; }
        private Mock<ISemaphoreFactory> MockSemaphoreFactory { get; set; }
        private Mock<ISemaphore> MockSemaphore { get; set; }
        
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
                .Returns(Data);
            
            MockFile = new Mock<IFile>();
            MockFile
                .Setup(x => x.OpenWrite(Filename))
                .Returns(MockWriteStream.Object);
            MockFile
                .Setup(x => x.OpenRead(Filename))
                .Returns(MockReadStream.Object);
            MockFile
                .Setup(x => x.Exists(Filename))
                .Returns(true);

            MockSemaphore = new Mock<ISemaphore>();
            MockSemaphore
                .Setup(x => x.Wait(It.Is<TimeSpan>(y => Math.Abs(y.TotalMinutes - 1.0) < 1)))
                .Returns(true);
            
            MockSemaphoreFactory = new Mock<ISemaphoreFactory>();
            MockSemaphoreFactory
                .Setup(x => x.Create(1, 1))
                .Returns(MockSemaphore.Object);
            
            FileStreamer = new FileStreamer(MockMessagePackSerializer.Object,MockFile.Object, MockSemaphoreFactory.Object);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool GetLockForFileWaitFails()
        {
            MockSemaphore
                .Setup(x => x.Wait(It.Is<TimeSpan>(y => Math.Abs(y.TotalMinutes - 1) < 1)))
                .Returns(false);
            var result = FileStreamer.GetLockForFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1,1), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool GetLockForFileWaitSuccess()
        {
            var result = FileStreamer.GetLockForFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1,1), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool GetLockForFileCreateOnlyOneSemaphore()
        {
            FileStreamer.GetLockForFile(Filename);
            var result = FileStreamer.GetLockForFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1,1), Times.Once);
            return result;
        }
        
        [Test(Author = "PackDB Creator")]
        public void UnlockFileWhenThereIsNoLock()
        {
            FileStreamer.UnlockFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1,1), Times.Never);
            MockSemaphore.Verify(x => x.Release(), Times.Never);
        }

        [Test(Author = "PackDB Creator")]
        public void UnlockFileWhenThereIsALock()
        {
            FileStreamer.GetLockForFile(Filename);
            FileStreamer.UnlockFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1,1), Times.Once);
            MockSemaphore.Verify(x => x.Release(), Times.Once);
        }

        [Test(Author = "PackDB Creator")]
        public void UnlockFileWhenThereIsALockMultipleTimes()
        {
            FileStreamer.GetLockForFile(Filename);
            FileStreamer.UnlockFile(Filename);
            FileStreamer.UnlockFile(Filename);
            MockSemaphoreFactory.Verify(x => x.Create(1,1), Times.Once);
            MockSemaphore.Verify(x => x.Release(), Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteDataToStreamNewStreamNeeded()
        {
            var result = FileStreamer.WriteDataToStream(Filename, Data);
            MockFile.Verify(x => x.OpenWrite(Filename), Times.Once);
            MockMessagePackSerializer.Verify(x => x.Serialize(WriteStream, Data), Times.Once);
            return result;
        }
        
        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool WriteDataToStreamMultipleTimes()
        {
            FileStreamer.WriteDataToStream(Filename, Data);
            var result = FileStreamer.WriteDataToStream(Filename, Data);
            MockFile.Verify(x => x.OpenWrite(Filename), Times.Once);
            MockMessagePackSerializer.Verify(x => x.Serialize(WriteStream, Data), Times.Exactly(2));
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void ReadDataFromStreamNewStreamNeeded()
        {
            Assert.AreSame(Data,FileStreamer.ReadDataFromStream<BasicData>(Filename));
            MockFile.Verify(x => x.OpenRead(Filename), Times.Once);
            MockReadStream.Verify(x => x.Close(), Times.Once);
            MockReadStream.Verify(x => x.Dispose(), Times.Once);
            MockMessagePackSerializer.Verify(x => x.Deserialize<BasicData>(ReadStream), Times.Once);
        }
        
        [Test(Author = "PackDB Creator")]
        public void ReadDataFromStreamMultipleTimes()
        {
            FileStreamer.ReadDataFromStream<BasicData>(Filename);
            Assert.AreSame(Data,FileStreamer.ReadDataFromStream<BasicData>(Filename));
            MockFile.Verify(x => x.OpenRead(Filename), Times.Exactly(2));
            MockMessagePackSerializer.Verify(x => x.Deserialize<BasicData>(ReadStream), Times.Exactly(2));
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CloseStreamWhenThereIsNoStream()
        {
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            return result;
        }
        
        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool CloseStreamWhenThereIsAWriteStream()
        {
            FileStreamer.WriteDataToStream(Filename, Data);
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Once);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Once);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CloseStreamWhenThereIsAWriteStreamIsAlreadyClosed()
        {
            FileStreamer.WriteDataToStream(Filename, Data);
            FileStreamer.CloseStream(Filename);
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Once);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Once);
            MockReadStream
                .Verify(x => x.Close(), Times.Never);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool CloseStreamWhenThereIsAReadStreamIsAlreadyClosed()
        {
            FileStreamer.ReadDataFromStream<BasicData>(Filename);
            FileStreamer.CloseStream(Filename);
            var result = FileStreamer.CloseStream(Filename);
            MockWriteStream
                .Verify(x => x.Close(), Times.Never);
            MockWriteStream
                .Verify(x => x.Dispose(), Times.Never);
            MockReadStream
                .Verify(x => x.Close(), Times.Once);
            MockReadStream
                .Verify(x => x.Dispose(), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator")]
        public void DisposeOfStreamWhenThereIsNoStream()
        {
            FileStreamer.DisposeOfStream(Filename);
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
        public void DisposeOfStreamWhenThereIsAWriteStream()
        {
            FileStreamer.WriteDataToStream(Filename, Data);
            FileStreamer.DisposeOfStream(Filename);
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
        public void DisposeOfStreamWhenThereIsAReadStream()
        {
            FileStreamer.ReadDataFromStream<BasicData>(Filename);
            FileStreamer.DisposeOfStream(Filename);
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
        public void DisposeOfStreamWhenThereIsAWriteStreamIsAlreadyDisposed()
        {
            FileStreamer.WriteDataToStream(Filename, Data);
            FileStreamer.DisposeOfStream(Filename);
            FileStreamer.DisposeOfStream(Filename);
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
        public void DisposeOfStreamWhenThereIsAReadStreamIsAlreadyDisposed()
        {
            FileStreamer.ReadDataFromStream<BasicData>(Filename);
            FileStreamer.DisposeOfStream(Filename);
            FileStreamer.DisposeOfStream(Filename);
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
        public bool ExistsWhenItDoesNotExist()
        {
            MockFile
                .Setup(x => x.Exists(Filename))
                .Returns(false);
            return FileStreamer.Exists(Filename);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool ExistsWhenItDoesExist()
        {
            return FileStreamer.Exists(Filename);
        }

        [Test(Author = "PackDB Creator")]
        public void Delete()
        {
            FileStreamer.Delete(Filename);
            MockFile.Verify(x => x.Delete(Filename), Times.Once);
        }
        
        [Test(Author = "PackDB Creator")]
        public void SoftDelete()
        {
            FileStreamer.SoftDelete(Filename);
            MockFile.Verify(x => x.Move(Filename,Filename + ".deleted"), Times.Once);
        }
        
        [Test(Author = "PackDB Creator")]
        public void Undelete()
        {
            FileStreamer.Undelete(Filename);
            MockFile.Verify(x => x.Move(Filename + ".deleted", Filename), Times.Once);
        }
    }
}