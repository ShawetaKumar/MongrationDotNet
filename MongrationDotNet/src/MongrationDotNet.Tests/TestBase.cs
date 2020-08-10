using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongrationDotNet.Tests
{
    public class TestBase
    {
        public const string DbName = "dbName";
        public const string CollectionName = "product";
        public MongoDbRunner runner;
        public MigrationRunner MigrationRunner;
        public IMongoDatabase Database;


        [OneTimeSetUp]
        public void Setup()
        {
            runner = MongoDbRunner.Start();
            var client = new MongoClient(runner.ConnectionString);
            Database = client.GetDatabase(DbName);
            MigrationRunner = new ServiceCollection()
                .AddMigration(Database);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (!runner.Disposed)
                runner.Dispose();
        }
    }
}