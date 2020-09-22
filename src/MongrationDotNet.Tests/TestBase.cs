using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongrationDotNet.Tests
{
    public class TestBase
    {
        private const string NugetPackages = "NUGET_PACKAGES";
        public const string DbName = "dbName";
        public const string CollectionName = "product";
        public IMongoDatabase Database;
        public IMigrationRunner MigrationRunner;
        public MongoDbRunner runner;
        public static string FilePath = $"{Directory.GetCurrentDirectory()}//Data//product.json";


        [OneTimeSetUp]
        public void Setup()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(NugetPackages)))
            {
                var nugetDirectory = Environment.GetEnvironmentVariable(NugetPackages);
                runner ??= MongoDbRunner.Start(binariesSearchDirectory: nugetDirectory);
            }
            else
            {
                runner ??= MongoDbRunner.Start();
            }

            var client = new MongoClient(runner.ConnectionString);
            Database = client.GetDatabase(DbName);
            var option = Options.Create(new MigrationConcurrencyOptions()
            {
                WaitInterval = 500,
                TimeoutCount = 5
            });

            var serviceProvider = new ServiceCollection()
                .Configure<MigrationConcurrencyOptions>(options =>
                {
                    options.WaitInterval = 500;
                    options.TimeoutCount = 5;
                })
                .AddMigration(Database)
                .WithAllAvailableMigrations()
                .BuildServiceProvider();

            MigrationRunner = serviceProvider.GetService<IMigrationRunner>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (!runner.Disposed)
                runner.Dispose();
        }
    }
}