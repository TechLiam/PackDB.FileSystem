using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Indexing;
using PackDB.FileSystem.AuditWorker;
using PackDB.FileSystem.IndexWorker;

namespace PackDB.FileSystem.Tests
{
    [TestFixture]
    [TestOf(typeof(FileAuditWorker))]
    [ExcludeFromCodeCoverage]
    public class IndexWorkerTester
    {
        [SetUp]
        public void Setup()
        {
            Randomizer = Randomizer.CreateRandomizer();

            IndexKey = Randomizer.GetString();
            ExpectedIds = new List<int>
            {
                Randomizer.Next(),
                Randomizer.Next(),
                Randomizer.Next()
            };

            IndexData = new IndexableData
            {
                Id = Randomizer.Next(),
                IndexValue1 = Randomizer.GetString()
            };

            UniqueIndexData = new UniqueIndexableData()
            {
                Id = Randomizer.Next(),
                IndexValue1 = Randomizer.GetString()
            };

            MockFileStreamer = new Mock<IFileStreamer>();
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.Exists("Data\\UniqueIndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>
                {
                    Keys = new List<IndexKey<string>>
                    {
                        new IndexKey<string>
                        {
                            Value = IndexKey,
                            Ids = ExpectedIds
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexKey,
                            Ids = ExpectedIds
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\UniqueIndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexKey,
                            Ids = new List<int>()
                            {
                                UniqueIndexData.Id
                            }
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(
                    "Data\\IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y =>
                        y.Keys.Count() == 2 &&
                        y.Keys.ElementAt(0).Value.Equals(IndexKey) &&
                        y.Keys.ElementAt(1).Value.Equals(IndexData.IndexValue1) &&
                        y.Keys.ElementAt(0).Ids.Count() == 3 &&
                        y.Keys.ElementAt(1).Ids.Count() == 1 &&
                        y.Keys.ElementAt(1).Ids.ElementAt(0) == IndexData.Id
                    )
                ))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y => y.Keys.Count() == 1 &&
                                              y.Keys.ElementAt(0).Value.Equals(IndexData.IndexValue1) &&
                                              y.Keys.ElementAt(0).Ids.Count() == 1 &&
                                              y.Keys.ElementAt(0).Ids.ElementAt(0) == IndexData.Id)))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(
                    "Data\\IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y =>
                        y.Keys.Count() == 1 &&
                        y.Keys.ElementAt(0).Value.Equals(IndexKey) &&
                        y.Keys.ElementAt(0).Ids.Count() == 4 &&
                        y.Keys.ElementAt(0).Ids.ElementAt(0) == ExpectedIds.ElementAt(0) &&
                        y.Keys.ElementAt(0).Ids.ElementAt(1) == ExpectedIds.ElementAt(1) &&
                        y.Keys.ElementAt(0).Ids.ElementAt(2) == ExpectedIds.ElementAt(2) &&
                        y.Keys.ElementAt(0).Ids.ElementAt(3) == IndexData.Id
                    )
                ))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);

            FileIndexWorker = new FileIndexWorker(MockFileStreamer.Object);
        }

        private FileIndexWorker FileIndexWorker { get; set; }
        private string IndexKey { get; set; }
        private List<int> ExpectedIds { get; set; }
        private IndexableData IndexData { get; set; }
        private UniqueIndexableData UniqueIndexData { get; set; }
        private Randomizer Randomizer { get; set; }
        private Mock<IFileStreamer> MockFileStreamer { get; set; }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> IndexExistWhenItDoesNot()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(false);
            return await FileIndexWorker.IndexExist<IndexableData>("IndexValue1");
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexExistWhenItDoes()
        {
            return await FileIndexWorker.IndexExist<IndexableData>("IndexValue1");
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetIdsFromIndexWhenReadDataFromStreamReturnsAnEmptyIndex()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>());
            var data = FileIndexWorker.GetIdsFromIndex<IndexableData, string>("IndexValue1", "Unit test");
            var result = new List<int>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithNoKeys()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>
                {
                    Keys = new List<IndexKey<string>>()
                });
            var data = FileIndexWorker.GetIdsFromIndex<IndexableData, string>("IndexValue1", "Unit test");
            var result = new List<int>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithOutTheKey()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>
                {
                    Keys = new List<IndexKey<string>>
                    {
                        new IndexKey<string>
                        {
                            Value = Randomizer.GetString()
                        }
                    }
                });
            var data = FileIndexWorker.GetIdsFromIndex<IndexableData, string>("IndexValue1", "Unit test");
            var result = new List<int>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithTheKeyButNoIds()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>
                {
                    Keys = new List<IndexKey<string>>
                    {
                        new IndexKey<string>
                        {
                            Value = IndexKey,
                            Ids = new List<int>()
                        }
                    }
                });
            var data = FileIndexWorker.GetIdsFromIndex<IndexableData, string>("IndexValue1", IndexKey);
            var result = new List<int>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetIdsFromIndexWhenReadDataFromStreamReturnsAnIndexWithTheKeyAndIds()
        {
            var data = FileIndexWorker.GetIdsFromIndex<IndexableData, string>("IndexValue1", IndexKey);
            var result = new List<int>();
            await foreach (var d in data) result.Add(d);
            Assert.AreEqual(ExpectedIds.Count(), result.Count());
            for (var i = 0; i < ExpectedIds.Count(); i++) Assert.IsTrue(result.Contains(ExpectedIds[i]));
        }
        
        [Test(Author = "PackDB Creator")]
        public async Task GetKeysFromIndexWhenReadDataFromStreamReturnsAnEmptyIndex()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>());
            var data = FileIndexWorker.GetKeysFromIndex<IndexableData, string>("IndexValue1");
            var result = new List<IndexKey<string>>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetKeysFromIndexWhenReadDataFromStreamReturnsAnIndexWithNoKeys()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<string>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<string>
                {
                    Keys = new List<IndexKey<string>>()
                });
            var data = FileIndexWorker.GetKeysFromIndex<IndexableData, string>("IndexValue1");
            var result = new List<IndexKey<string>>();
            await foreach (var d in data) result.Add(d);
            Assert.IsEmpty(result);
        }

        [Test(Author = "PackDB Creator")]
        public async Task GetKeysFromIndexWhenReadDataFromStreamReturnsAnIndexWithTheKeyAndIds()
        {
            var data = FileIndexWorker.GetKeysFromIndex<IndexableData, string>("IndexValue1");
            var result = new List<IndexKey<string>>();
            await foreach (var d in data) result.Add(d);
            Assert.AreEqual(1, result.Count(x => x.Value == IndexKey));
        }
        
        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexDataTypeWithNoIndexedProperties()
        {
            return await FileIndexWorker.Index(new BasicData());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexWhenIndexDoesNotExist()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(false);
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x => x.WriteDataToStream("Data\\IndexableData\\IndexValue1.index",
                        It.Is<Index<object>>(y => y.Keys.Count() == 1 &&
                                                  y.Keys.ElementAt(0).Value.Equals(IndexData.IndexValue1) &&
                                                  y.Keys.ElementAt(0).Ids.Count() == 1 &&
                                                  y.Keys.ElementAt(0).Ids.ElementAt(0) == IndexData.Id)),
                    Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexWhenIndexDoesExistButTheKeyDoesNot()
        {
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "Data\\IndexableData\\IndexValue1.index",
                        It.Is<Index<object>>(y =>
                            y.Keys.Count() == 2 &&
                            y.Keys.ElementAt(0).Value.Equals(IndexKey) &&
                            y.Keys.ElementAt(1).Value.Equals(IndexData.IndexValue1) &&
                            y.Keys.ElementAt(0).Ids.Equals(ExpectedIds) &&
                            y.Keys.ElementAt(1).Ids.Count() == 1 &&
                            y.Keys.ElementAt(1).Ids.ElementAt(0) == IndexData.Id
                        )
                    ), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexWhenIndexDoesExistAndTheKey()
        {
            IndexData.IndexValue1 = IndexKey;
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "Data\\IndexableData\\IndexValue1.index",
                        It.Is<Index<object>>(y =>
                            y.Keys.Count() == 1 &&
                            y.Keys.ElementAt(0).Value.Equals(IndexKey) &&
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
        public async Task<bool> IndexWhenIndexDoesExistAndTheKeyIsUniqueAndAlreadySet()
        {
            UniqueIndexData.IndexValue1 = IndexKey;
            var result = await FileIndexWorker.Index(UniqueIndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        It.IsAny<string>(),
                        It.IsAny<Index<object>>()
                    ), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> IndexWhenIndexDoesExistAndTheKeyIsUniqueAndAlreadyUsed()
        {
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\UniqueIndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexKey,
                            Ids = new List<int>()
                            {
                                Randomizer.Next()
                            }
                        }
                    }
                });
            UniqueIndexData.IndexValue1 = IndexKey;
            var result = await FileIndexWorker.Index(UniqueIndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        It.IsAny<string>(),
                        It.IsAny<Index<object>>()
                    ), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexWhenIndexDoesExistAndTheKeyHasTheIdAlready()
        {
            IndexData.IndexValue1 = IndexKey;
            ExpectedIds.Add(IndexData.Id);
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "IndexableData\\IndexValue1.index",
                        It.IsAny<Index<object>>()
                    ), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> IndexWhenIndexDoesExistAndTheIdBelongToAnotherKey()
        {
            ExpectedIds.Add(IndexData.Id);
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "Data\\IndexableData\\IndexValue1.index",
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
        public async Task<bool> IndexWhenWriteDataToStreamFails()
        {
            MockFileStreamer
                .Setup(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<Index<object>>()))
                .ReturnsAsync(false);
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "Data\\IndexableData\\IndexValue1.index",
                        It.IsAny<Index<object>>()
                    ), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> IndexWhenCloseStreamFails()
        {
            MockFileStreamer
                .Setup(x => x.CloseStream(It.IsAny<string>()))
                .ReturnsAsync(false);
            var result = await FileIndexWorker.Index(IndexData);
            MockFileStreamer
                .Verify(x =>
                    x.WriteDataToStream(
                        "Data\\IndexableData\\IndexValue1.index",
                        It.IsAny<Index<object>>()
                    ), Times.Once);
            MockFileStreamer
                .Verify(x =>
                    x.CloseStream(
                        "Data\\IndexableData\\IndexValue1.index"
                    ), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexDataTypeWithNoIndexedProperties()
        {
            return await FileIndexWorker.Unindex(new BasicData());
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexDoesNotExist()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(false);
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.ReadDataFromStream<Index<object>>(It.IsAny<string>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexIsEmpty()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>());
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<Index<object>>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexHasNoKeys()
        {
            MockFileStreamer
                .Setup(x => x.Exists("IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>()
                });
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<Index<object>>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexHasKeysButNotTheOneNeeded()
        {
            MockFileStreamer
                .Setup(x => x.Exists("IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = Randomizer.GetString(),
                            Ids = new List<int>()
                        }
                    }
                });
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<Index<object>>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexHasKeysButNotTheId()
        {
            MockFileStreamer
                .Setup(x => x.Exists("IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                Randomizer.Next()
                            }
                        }
                    }
                });
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.WriteDataToStream(It.IsAny<string>(), It.IsAny<Index<object>>()), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UnindexWhenIndexHasKeyAndIdButWriteFails()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                Randomizer.Next(),
                                IndexData.Id
                            }
                        }
                    }
                });
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(
                    x => x.WriteDataToStream("Data\\IndexableData\\IndexValue1.index",
                        It.Is<Index<object>>(y => y.Keys.Count() == 1)), Times.Once);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"), Times.Never);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UnindexWhenIndexHasKeyAndIdButCloseFails()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                Randomizer.Next(),
                                IndexData.Id
                            }
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y => y.Keys.Count() == 1)))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(false);
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexHasKeyAndIdAndSaveSuccess()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                Randomizer.Next(),
                                IndexData.Id
                            }
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y => y.Keys.Count() == 1)))
                .ReturnsAsync(true);
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexHasKeyAndOnlyTheIdAndSaveSuccess()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                IndexData.Id
                            }
                        },
                        new IndexKey<object>
                        {
                            Value = Randomizer.GetString(),
                            Ids = new List<int>
                            {
                                Randomizer.Next()
                            }
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.WriteDataToStream("Data\\IndexableData\\IndexValue1.index",
                    It.Is<Index<object>>(y => y.Keys.Count() == 1)))
                .ReturnsAsync(true);
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = false)]
        public async Task<bool> UnindexWhenIndexHasOnlyTheKeyAndOnlyTheIdButDeletedIndexFails()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                IndexData.Id
                            }
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.Delete("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(false);
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"), Times.Never);
            MockFileStreamer
                .Verify(x => x.Delete("Data\\IndexableData\\IndexValue1.index"), Times.Once);
            return result;
        }

        [Test(Author = "PackDB Creator", ExpectedResult = true)]
        public async Task<bool> UnindexWhenIndexHasOnlyTheKeyAndOnlyTheIdTheIndexIsDeleted()
        {
            MockFileStreamer
                .Setup(x => x.Exists("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            MockFileStreamer
                .Setup(x => x.ReadDataFromStream<Index<object>>("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(new Index<object>
                {
                    Keys = new List<IndexKey<object>>
                    {
                        new IndexKey<object>
                        {
                            Value = IndexData.IndexValue1,
                            Ids = new List<int>
                            {
                                IndexData.Id
                            }
                        }
                    }
                });
            MockFileStreamer
                .Setup(x => x.Delete("Data\\IndexableData\\IndexValue1.index"))
                .ReturnsAsync(true);
            var result = await FileIndexWorker.Unindex(IndexData);
            MockFileStreamer
                .Verify(x => x.CloseStream("Data\\IndexableData\\IndexValue1.index"), Times.Never);
            MockFileStreamer
                .Verify(x => x.Delete("Data\\IndexableData\\IndexValue1.index"), Times.Once);
            return result;
        }
    }
}