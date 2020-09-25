using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongrationDotNet.Tests
{
    public class TestBase
    {
        private const string NugetPackages = "NUGET_PACKAGES";
        public const string DbName = "dbName";
        public const string CollectionName = "product";
        public TimeSpan DefaultMigrationExpiry = TimeSpan.FromMinutes(2);
        public IMongoDatabase Database;
        public IMigrationRunner MigrationRunner;
        public MongoDbRunner Runner;
        public static string FilePath = $"{Directory.GetCurrentDirectory()}//Data//product.json";
        protected IMongoCollection<MigrationDetails> MigrationCollection;


        [OneTimeSetUp]
        public async Task Setup()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(NugetPackages)))
            {
                var nugetDirectory = Environment.GetEnvironmentVariable(NugetPackages);
                Runner ??= MongoDbRunner.Start(binariesSearchDirectory: nugetDirectory);
            }
            else
            {
                Runner ??= MongoDbRunner.Start();
            }

            var client = new MongoClient(Runner.ConnectionString);
            Database = client.GetDatabase(DbName);

            var serviceProvider = new ServiceCollection()
                .AddMigration(Database)
                .WithAllAvailableMigrations()
                .BuildServiceProvider();

            MigrationRunner = serviceProvider.GetService<IMigrationRunner>();
            await SetupMigrationDetailsCollection();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (!Runner.Disposed)
                Runner.Dispose();
        }

        public async Task SetupMigrationDetailsCollection()
        {
            await Database.CreateCollectionAsync(Constants.MigrationDetailsCollection);
            MigrationCollection = Database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
            var indexModel = new[]
            {
                new CreateIndexModel<MigrationDetails>(
                    Builders<MigrationDetails>.IndexKeys.Ascending(x => x.Status)),
                new CreateIndexModel<MigrationDetails>(
                    Builders<MigrationDetails>.IndexKeys.Descending(x => x.Status)),

            };
            await MigrationCollection.Indexes.CreateManyAsync(indexModel);
        }

        public async Task ResetMigrationDetails()
        {
            await Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
        }
    }
}