using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PackDB.Core.Auditing;

namespace PackDB.Core.Tests
{
    [TestFixture,TestOf(typeof(DataManager)),ExcludeFromCodeCoverage]
    public class AuditGeneratorTester
    {
        
        private AuditGenerator AuditGenerator { get; set; }
        private Randomizer Randomizer { get; set; }
        
        [SetUp]
        public void Setup()
        {
            AuditGenerator = new AuditGenerator();
            Randomizer = Randomizer.CreateRandomizer();
        }

        [Test(Author = "PackDB Creator")]
        public void NewLog()
        {
            var data = new AuditedEntity()
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString()
            };
            var auditLog = AuditGenerator.NewLog(data);
            Assert.AreEqual(1, auditLog.Entries.Count());
            Assert.AreEqual(AuditType.Create, auditLog.Entries.ElementAt(0).Type);
            Assert.AreEqual(2, auditLog.Entries.ElementAt(0).Changes.Count());
            Assert.IsTrue(auditLog.Entries.ElementAt(0).Changes.Any(x => x.PropertyName == "Id" && x.NewValue.Equals(data.Id) && x.OldValue == null));
            Assert.IsTrue(auditLog.Entries.ElementAt(0).Changes.Any(x => x.PropertyName == "Value1" && x.NewValue.Equals(data.Value1) && x.OldValue == null));
        }
        
        [Test(Author = "PackDB Creator")]
        public void UpdateLog()
        {
            var data = new AuditedEntity()
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString()
            };
            var oldData = new AuditedEntity()
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString()
            };
            var auditLog = AuditGenerator.UpdateLog(data,oldData,AuditGenerator.NewLog(data));
            Assert.AreEqual(2, auditLog.Entries.Count());
            Assert.AreEqual(AuditType.Update, auditLog.Entries.ElementAt(1).Type);
            Assert.AreEqual(2, auditLog.Entries.ElementAt(1).Changes.Count());
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Id" && x.NewValue.Equals(data.Id) && x.OldValue.Equals(oldData.Id)));
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Value1" && x.NewValue.Equals(data.Value1) && x.OldValue.Equals(oldData.Value1)));
        }

        [Test(Author = "PackDB Creator")]
        public void DeleteLog()
        {
            var data = new AuditedEntity()
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString()
            };
            var auditLog = AuditGenerator.DeleteLog(data,AuditGenerator.NewLog(data));
            Assert.AreEqual(2, auditLog.Entries.Count());
            Assert.AreEqual(AuditType.Delete, auditLog.Entries.ElementAt(1).Type);
            Assert.AreEqual(2, auditLog.Entries.ElementAt(1).Changes.Count());
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Id" && x.NewValue == null && x.OldValue.Equals(data.Id)));
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Value1" && x.NewValue == null && x.OldValue.Equals(data.Value1)));
        }

        [Test(Author = "PackDB Creator")]
        public void UndeleteLog()
        {
            var data = new AuditedEntity()
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString()
            };
            var auditLog = AuditGenerator.UndeleteLog(data,AuditGenerator.NewLog(data));
            Assert.AreEqual(2, auditLog.Entries.Count());
            Assert.AreEqual(AuditType.Undelete, auditLog.Entries.ElementAt(1).Type);
            Assert.AreEqual(2, auditLog.Entries.ElementAt(1).Changes.Count());
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Id" && x.NewValue.Equals(data.Id) && x.OldValue == null));
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Value1" && x.NewValue.Equals(data.Value1) && x.OldValue == null));
        }
        
        [Test(Author = "PackDB Creator")]
        public void RollbackLog()
        {
            var data = new AuditedEntity()
            {
                Id = Randomizer.Next(),
                Value1 = Randomizer.GetString()
            };
            var auditLog = AuditGenerator.RollbackLog(data,AuditGenerator.NewLog(data));
            Assert.AreEqual(2, auditLog.Entries.Count());
            Assert.AreEqual(AuditType.Rollback, auditLog.Entries.ElementAt(1).Type);
            Assert.AreEqual(2, auditLog.Entries.ElementAt(1).Changes.Count());
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Id" && x.NewValue.Equals(data.Id) && x.OldValue == null));
            Assert.IsTrue(auditLog.Entries.ElementAt(1).Changes.Any(x => x.PropertyName == "Value1" && x.NewValue.Equals(data.Value1) && x.OldValue == null));
        }
    }
}