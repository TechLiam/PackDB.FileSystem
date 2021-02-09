using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Indexing;
using PackDB.FileSystem.AuditWorker;
using PackDB.FileSystem.IndexWorker;

namespace PackDB.FileSystem.Tests
{
    [TestFixture,TestOf(typeof(FileAuditWorker)),ExcludeFromCodeCoverage]
    public class IndexWorkerTester
    {
        private FileIndexWorker FileIndexWorker { get; set; }
        private string IndexKey { get; set; }
        private List<int> ExpectedIds { get; set; }
        private IndexableData IndexData { get; set; }
        private Randomizer Randomizer { get; set; }
        private Mock<IFileStreamer> MockFileStreamer { get; set; }

        [SetUp]
        public void Setup()
        {
            Randomizer = Randomizer.CreateRandomizer();

            IndexKey = Randomizer.GetString();
            ExpectedIds = new List<int>()
            {
                Randomizer.Next(),
                Randomizer.Next(),
                Randomizer.Next()
            };

            IndexData = new IndexableData()
            {
                Id = Randomizer.Next(),
                IndexValue1 = Randomizer.GetString()
            };
            
            MockFileStreamer = new Mock<IFileStreamer>();
            MockFileStreamer
                .Setup(x => x.Exists($"IndexableData\\IndexValue1.index"))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>($"IndexableData\\IndexValue1.index"))
                .Returns(new Index<string>()
                {
                    Keys = new List<IndexKey<string>>()
                    {
                        new IndexKey<string>()
                        {
                            Value = IndexKey,
                            Ids = ExpectedIds
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>($"IndexableData\\IndexValue1.index"))
                .Returns(new Index<object>()
                {
                    Keys = new List<IndexKey<object>>()
                    {
                        new IndexKey<object>()
                        {
                            Value = IndexKey,
                            Ids = ExpectedIds
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(
                    "IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y =>
                        y.Keys.Count() == 2 &&
                        y.Keys.ElementAt(0).Value == IndexKey &&
                        y.Keys.ElementAt(1).Value == IndexData.IndexValue1 &&
                        y.Keys.ElementAt(0).Ids.Count() == 3 &&
                        y.Keys.ElementAt(1).Ids.Count() == 1 &&
                        y.Keys.ElementAt(1).Ids.ElementAt(0) == IndexData.Id
                    )
                ))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("IndexableData\\IndexValue1.index", 
                    It.Is<Index<object>>(y => y.Keys.Count() == 1 &&
                                              y.Keys.ElementAt(0).Value == IndexData.IndexValue1 &&
                                              y.Keys.ElementAt(0).Ids.Count() == 1 &&
                                              y.Keys.ElementAt(0).Ids.ElementAt(0) == IndexData.Id)))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index", 
                        It.Is<Index<object>>(y => 
                            y.Keys.Count() == 1 &&
                            y.Keys.ElementAt(0).Value == IndexKey &&
                            y.Keys.ElementAt(0).Ids.Count() == 4 &&
                            y.Keys.ElementAt(0).Ids.ElementAt(0) == ExpectedIds.ElementAt(0) &&
                            y.Keys.ElementAt(0).Ids.ElementAt(1) == ExpectedIds.ElementAt(1) &&
                            y.Keys.ElementAt(0).Ids.ElementAt(2) == ExpectedIds.ElementAt(2) &&
                            y.Keys.ElementAt(0).Ids.ElementAt(3) == IndexData.Id
                        )
                    ))
                .Returns(true);
            MockFileStreamer
                .Setup(x => x.CloseStream("IndexableData\\IndexValue1.index"))
                .Returns(true);
            
            FileIndexWorker = new FileIndexWorker(MockFileStreamer.Object);
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool IndexExistWhenItDoesNot()
        {
            MockFileStreamer
                .Setup(x => x.Exists($"IndexableData\\IndexValue1.index"))
                .Returns(false);
            return FileIndexWorker.IndexExist<IndexableData>("IndexValue1");
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool IndexExistWhenItDoes()
        {
            return FileIndexWorker.IndexExist<IndexableData>("IndexValue1");
        }
        
        [Test(Author = "PackDB Creator")]
        public void GetIdsFromIndexWhenReadDataFromStreamReturnsAnEmptyIndex()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>($"IndexableData\\IndexValue1.index"))
                .Returns(new Index<string>());
            var result = FileIndexWorker.GetIdsFromIndex<IndexableData,string>("IndexValue1","Unit test");
            Assert.IsEmpty(result);
        }
        
        [Test(Author = "PackDB Creator")]
        public void GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithNoKeys()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>($"IndexableData\\IndexValue1.index"))
                .Returns(new Index<string>()
                {
                    Keys = new List<IndexKey<string>>()
                });
            var result = FileIndexWorker.GetIdsFromIndex<IndexableData,string>("IndexValue1","Unit test");
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public void GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithOutTheKey()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>($"IndexableData\\IndexValue1.index"))
                .Returns(new Index<string>()
                {
                    Keys = new List<IndexKey<string>>()
                    {
                        new IndexKey<string>()
                        {
                            Value = Randomizer.GetString()
                        }
                    }
                });
            var result = FileIndexWorker.GetIdsFromIndex<IndexableData,string>("IndexValue1","Unit test");
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public void GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithTheKeyButNoIds()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>($"IndexableData\\IndexValue1.index"))
                .Returns(new Index<string>()
                {
                    Keys = new List<IndexKey<string>>()
                    {
                        new IndexKey<string>()
                        {
                            Value = IndexKey,
                            Ids = new List<int>()
                        }
                    }
                });
            var result = FileIndexWorker.GetIdsFromIndex<IndexableData,string>("IndexValue1",IndexKey);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public void GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithTheKeyAndIds()
        {
            var result = FileIndexWorker.GetIdsFromIndex<IndexableData,string>("IndexValue1",IndexKey).ToArray();
            Assert.AreEqual(ExpectedIds.Count() ,result.Count());
            for (var i = 0; i < ExpectedIds.Count(); i++)
            {
                Assert.IsTrue(result.Contains(ExpectedIds[i]));
            }
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool IndexDataTypeWithNoIndexedProperties()
        {
            return FileIndexWorker.Index(new BasicData());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool IndexWhenIndexDoesNotExist()
        {
            MockFileStreamer
                .Setup(x => x.Exists("IndexableData\\IndexValue1.index"))
                .Returns(false);
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => x.WriteDataToStream("IndexableData\\IndexValue1.index", 
                    It.Is<Index<object>>(y => y.Keys.Count() == 1 &&
                                              y.Keys.ElementAt(0).Value == IndexData.IndexValue1 &&
                                              y.Keys.ElementAt(0).Ids.Count() == 1 &&
                                              y.Keys.ElementAt(0).Ids.ElementAt(0) == IndexData.Id)),
                    Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool IndexWhenIndexDoesExistButTheKeyDoesNot()
        {
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => 
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index", 
                        It.Is<Index<object>>(y => 
                            y.Keys.Count() == 2 &&
                            y.Keys.ElementAt(0).Value == IndexKey &&
                            y.Keys.ElementAt(1).Value == IndexData.IndexValue1 &&
                            y.Keys.ElementAt(0).Ids == ExpectedIds &&
                            y.Keys.ElementAt(1).Ids.Count() == 1 &&
                            y.Keys.ElementAt(1).Ids.ElementAt(0) == IndexData.Id
                        )
                    ), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool IndexWhenIndexDoesExistAndTheKey()
        {
            IndexData.IndexValue1 = IndexKey;
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => 
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index", 
                        It.Is<Index<object>>(y => 
                            y.Keys.Count() == 1 &&
                            y.Keys.ElementAt(0).Value == IndexKey &&
                            y.Keys.ElementAt(0).Ids.Count() == 4 &&
                            y.Keys.ElementAt(0).Ids.ElementAt(0) == ExpectedIds.ElementAt(0) &&
                            y.Keys.ElementAt(0).Ids.ElementAt(1) == ExpectedIds.ElementAt(1) &&
                            y.Keys.ElementAt(0).Ids.ElementAt(2) == ExpectedIds.ElementAt(2) &&
                            y.Keys.ElementAt(0).Ids.ElementAt(3) == IndexData.Id
                        )
                    ), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool IndexWhenIndexDoesExistAndTheKeyHasTheIdAlready()
        {
            IndexData.IndexValue1 = IndexKey;
            ExpectedIds.Add(IndexData.Id);
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => 
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index", 
                        It.IsAny<Index<object>>()
                    ), Times.Never);
            return result;
        }
        
        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public bool IndexWhenIndexDoesExistAndTheIdBelongToAnotherKey()
        {
            ExpectedIds.Add(IndexData.Id);
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => 
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index", 
                        It.Is<Index<object>>(y => 
                            y.Keys.Count() == 2 &&
                            y.Keys.ElementAt(0).Value.Equals(IndexKey) &&
                            y.Keys.ElementAt(1).Value.Equals(IndexData.IndexValue1) &&
                            y.Keys.ElementAt(0).Ids.Count() == 3 &&
                            y.Keys.ElementAt(1).Ids.Count() == 1 &&
                            y.Keys.ElementAt(1).Ids.ElementAt(0) == IndexData.Id
                        )
                    ), Times.Once);
            return result;
        }
        
        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool IndexWhenWriteDataToStreamFails()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<Index<object>>()))
                .Returns(false);
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => 
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index", 
                        It.IsAny<Index<object>>()
                    ), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public bool IndexWhenCloseStreamFails()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .Returns(false);
            var result = FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index",
                        It.IsAny<Index<object>>()
                    ), Times.Once);
            MockFileStreamer
                .Verify(x => 
                    x.CloseStream(
                        "IndexableData\\IndexValue1.index"
                    ), Times.Once);
            return result;
        }

    }
}