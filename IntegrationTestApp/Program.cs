using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PackDB.FileSystem;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace IntegrationTestApp
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<RemovePropertiesEnricher>()
                .WriteTo.Console(LogEventLevel.Verbose,"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}]{NewLine}\tScope:\t\t{Scope}{NewLine}\tMessage:\t{Message}{NewLine}\tProperties:\t{Properties}{NewLine}\tException:\t{Exception}{NewLine}")
                .MinimumLevel.Verbose()
                .CreateLogger();
            
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            var logger = serviceProvider.GetService<ILogger<Program>>();
            if (logger == null) return;
            using (logger.BeginScope("Integration testing app"))
            {
                //logger.LogInformation("PackDB integration testing stating");
                var dataManager = DataManagerFactory.CreateFileSystemDataManager(logger);

                var write = await dataManager.Write(new TestData
                {
                    Id = 1,
                    Firstname = "Test",
                    Lastname = "Person",
                    YearOfBirth = 1998
                });
                //logger.LogInformation(write ? "Saved data successfully" : "Save data failed");

                var read = await dataManager.Read<TestData>(1);
                if (read != null)
                {
                    //logger.LogInformation("Read data:");
                    //logger.LogInformation($"Id: {read.Id}");
                    //logger.LogInformation($"Name: {read.Firstname} {read.Lastname}");
                    //logger.LogInformation($"Year of birth: {read.YearOfBirth}");
                }

                var delete = await dataManager.Delete<TestData>(1);
                //logger.LogInformation(delete ? "Deleted the data" : "Failed to delete data");

                write = await dataManager.Write(new TestSoftDeleteData
                {
                    Id = 2,
                    Firstname = "Test",
                    Lastname = "Person",
                    YearOfBirth = 1985
                });
                //logger.LogInformation(write ? "Saved data successfully" : "Save data failed");

                delete = await dataManager.Delete<TestSoftDeleteData>(2);
                //logger.LogInformation(delete ? "Deleted the data" : "Failed to delete data");
                var restore = await dataManager.Restore<TestSoftDeleteData>(2);
                //logger.LogInformation(restore ? "Restored the data" : "Failed to restore data");

                write = await dataManager.Write(new TestIndexData
                {
                    Id = 2,
                    Firstname = "Test",
                    Lastname = "Person",
                    YearOfBirth = 1985,
                    PhoneNumber = "0123456789"
                });
                //logger.LogInformation(write ? "Saved data successfully" : "Save data failed");

                var indexData = dataManager.ReadIndex<TestIndexData, string>("0123456789", x => x.PhoneNumber);
                //logger.LogInformation("Read data with index");
                await foreach (var data in indexData)
                {
                    //logger.LogInformation($"Id: {data.Id}");
                    //logger.LogInformation($"Name: {data.Firstname} {data.Lastname}");
                    //logger.LogInformation($"Year of birth: {data.YearOfBirth}");
                    //logger.LogInformation($"Phone number: {data.PhoneNumber}");
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
                //logger.LogInformation(write ? "Saved data successfully" : "Save data failed");

                write = await dataManager.Write(auditData);
                //logger.LogInformation(write ? "Saved data successfully" : "Save data failed");

                //logger.LogInformation("PackDB integration testing finished");
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog())
                .AddTransient<Program>();
        }
        
        class RemovePropertiesEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent le, ILogEventPropertyFactory lepf)
            {
                le.RemovePropertyIfPresent("SourceContext");
            }
        }
    }
}