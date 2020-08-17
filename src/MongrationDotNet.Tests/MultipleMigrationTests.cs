using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class MultipleMigrationTests : TestBase
    {
        private IMongoCollection<MigrationDetails> migrationCollection;

        [SetUp]
        public void SetupDatabase()
        {
            migrationCollection = Database.GetCollection<MigrationDetails>(typeof(MigrationDetails).Name);
        }

        [TearDown]
        public void ResetMigrationDetails()
        {
            Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
        }

        [Test]
        public async Task Migration_ShouldApplyAllMigrations_WhenMultipleMigrationExists()
        {
            await MigrationRunner.Migrate();
            var collectionMigrations = await migrationCollection.Find(x => x.Type == Constants.DatabaseMigrationType)
                .ToListAsync();

            collectionMigrations.ShouldNotBeNull();
            collectionMigrations.Count.ShouldBe(2);

            var documentMigrations = await migrationCollection.Find(x => x.Type == Constants.CollectionMigrationType)
                .ToListAsync();

            documentMigrations.ShouldNotBeNull();
            documentMigrations.Count.ShouldBe(1);
        }
    }
}