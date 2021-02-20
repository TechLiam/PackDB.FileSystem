using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using PackDB.FileSystem;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("PackDB integration testing stating");

            var dataManager = DataManagerFactory.CreateFileSystemDataManager();

            var write = await dataManager.Write(new TestData
            {
                Id = 1,
                Firstname = "Test",
                Lastname = "Person",
                YearOfBirth = 1998
            });
            Console.WriteLine(write ? "Saved data successfully" : "Save data failed");

            var read = await dataManager.Read<TestData>(1);
            if (read != null)
            {
                Console.WriteLine("Read data:");
                Console.WriteLine($"Id: {read.Id}");
                Console.WriteLine($"Name: {read.Firstname} {read.Lastname}");
                Console.WriteLine($"Year of birth: {read.YearOfBirth}");
            }

            var delete = await dataManager.Delete<TestData>(1);
            Console.WriteLine(delete ? "Deleted the data" : "Failed to delete data");

            write = await dataManager.Write(new TestSoftDeleteData
            {
                Id = 2,
                Firstname = "Test",
                Lastname = "Person",
                YearOfBirth = 1985
            });
            Console.WriteLine(write ? "Saved data successfully" : "Save data failed");

            delete = await dataManager.Delete<TestSoftDeleteData>(2);
            Console.WriteLine(delete ? "Deleted the data" : "Failed to delete data");
            var restore = await dataManager.Restore<TestSoftDeleteData>(2);
            Console.WriteLine(restore ? "Restored the data" : "Failed to restore data");

            write = await dataManager.Write(new TestIndexData
            {
                Id = 2,
                Firstname = "Test",
                Lastname = "Person",
                YearOfBirth = 1985,
                PhoneNumber = "0123456789"
            });
            Console.WriteLine(write ? "Saved data successfully" : "Save data failed");

            var indexData = dataManager.ReadIndex<TestIndexData, string>("0123456789", x => x.PhoneNumber);
            Console.WriteLine("Read data with index");
            await foreach (var data in indexData)
            {
                Console.WriteLine($"Id: {data.Id}");
                Console.WriteLine($"Name: {data.Firstname} {data.Lastname}");
                Console.WriteLine($"Year of birth: {data.YearOfBirth}");
                Console.WriteLine($"Phone number: {data.PhoneNumber}");
            }

            await dataManager.Delete<TestIndexData>(2);

            var auditData = new TestAuditData
            {
                Id = 2,
                Firstname = "Test",
                Lastname = "Person",
                YearOfBirth = 1985
            };

            write = await dataManager.Write(auditData);
            Console.WriteLine(write ? "Saved data successfully" : "Save data failed");

            write = await dataManager.Write(auditData);
            Console.WriteLine(write ? "Saved data successfully" : "Save data failed");

            Console.WriteLine("PackDB integration testing finished");
        }
    }
}