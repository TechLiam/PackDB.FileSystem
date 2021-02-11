using System;
using PackDB.Core;
using PackDB.Core.Auditing;
using PackDB.Core.Locks;
using PackDB.Core.MessagePackProxy;
using PackDB.FileSystem;
using PackDB.FileSystem.AuditWorker;
using PackDB.FileSystem.DataWorker;
using PackDB.FileSystem.IndexWorker;
using PackDB.FileSystem.OS;

namespace IntegrationTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PackDB integration testing stating");

            var fileStreamer = new FileStreamer(new MessagePackSerializer(),new FileProxy(),new SemaphoreFactory());
            var dataWriter = new FileDataWorker(fileStreamer);
            var dataManager = new DataManager(dataWriter,new FileIndexWorker(fileStreamer),new FileAuditWorker(fileStreamer,new AuditGenerator()));

            var write = dataManager.Write(new TestData()
            {
                Id = 1,
                Firstname = "Test",
                Lastname = "Person",
                YearOfBirth = 1998
            });
            Console.WriteLine(write ? "Saved data successfully" : "Save data failed");

            var read = dataManager.Read<TestData>(1);
            Console.WriteLine("Read data:");
            Console.WriteLine($"Id: {read.Id}");
            Console.WriteLine($"Name: {read.Firstname} {read.Lastname}");
            Console.WriteLine($"Year of birth: {read.YearOfBirth}");

            var delete = dataManager.Delete<TestData>(1);
            Console.WriteLine(delete ? "Deleted the data" : "Failed to delete data");
            
            Console.WriteLine("PackDB integration testing finished");
        }
    }
}