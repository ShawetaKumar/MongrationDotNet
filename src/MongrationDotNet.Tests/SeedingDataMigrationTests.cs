using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class SeedingDataMigrationTests : TestBase
    {
        private Version Version => new Version(1, 1, 1, 6);

        [SetUp]
        public async Task SetupDatabase()
        {
            await Database.CreateCollectionAsync(CollectionName);
        }

        [TearDown]
        public async Task Reset()
        {
            await ResetMigrationDetails();
        }

        [Test]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();

            var result = await MigrationCollection.Find(x => x.Type == Constants.SeedingDataMigrationType)
                .FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(Version);
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyApplied()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.SeedingDataMigrationType, "initialize database");
            migrationDetails.MarkCompleted();
            await MigrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();

            var result = await MigrationCollection
                .Find(x => x.Version == Version && x.Type == Constants.SeedingDataMigrationType).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldInsertDocumentsInCollection_WhenSeedListContainDocuments()
        {
            await MigrationRunner.Migrate();

            var collection = Database.GetCollection<BsonDocument>("items");
            var documentsCount = await collection.Find(FilterDefinition<BsonDocument>.Empty).CountDocumentsAsync();
            
            documentsCount.ShouldBe(4);
        }
    }
}