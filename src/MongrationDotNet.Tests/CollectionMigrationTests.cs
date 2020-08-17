using System;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class CollectionMigrationTests : TestBase
    {
        private Version Version => new Version(1, 1, 1, 0);
        private IMongoCollection<MigrationDetails> migrationCollection;
        private IMongoCollection<BsonDocument> productCollection;

        [SetUp]
        public void SetupDatabase()
        {
            runner.Import(DbName, CollectionName, $"{Directory.GetCurrentDirectory()}\\Data\\product.json", true);
            migrationCollection = Database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
            productCollection = Database.GetCollection<BsonDocument>(CollectionName);
        }

        [TearDown]
        public void ResetMigrationDetails()
        {
            Database.DropCollection(Constants.MigrationDetailsCollection);
            Database.DropCollection(CollectionName);
        }

        [Test]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();
            var result = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty).FirstOrDefaultAsync();
            
            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(Version);
            result.Status.ShouldBe("Completed");
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationIsAlreadyInProgress()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.CollectionMigrationType, "product migration");
        
            await migrationCollection.InsertOneAsync(migrationDetails);
            
            await MigrationRunner.Migrate();
            var result = await migrationCollection.Find(x=> x.Type == Constants.CollectionMigrationType && x.Version == Version).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyApplied()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.CollectionMigrationType, "product migration");
            migrationDetails.MarkCompleted();
            
            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();
            var result = await migrationCollection.Find(x => x.Type == Constants.CollectionMigrationType && x.Version == Version).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldExecuteSuccessfullyAndNotThrowError_WhenMigrationObjectListContainsANonExistingField()
        {
            await MigrationRunner.Migrate();
            var result = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(Version);
        }

        [Test]
        public async Task Migration_ShouldRenameField_WhenMigrationObjectListContainsFieldsToRename()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
            var document = result.ToString();
            document.Contains("name").ShouldBeFalse();
            document.Contains("productName").ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldRenameEmbeddedField_WhenMigrationObjectListContainsEmbeddedFieldsToRename()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
            var document = result.ToString();
            document.Contains("\"store\" : { \"location\" : \"arizona\", \"id\" : \"s01\" }").ShouldBeFalse();
            document.Contains("\"store\" : { \"location\" : \"arizona\", \"code\" : \"s01\" }").ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldRemoveField_WhenMigrationObjectListContainsFieldsToRemove()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
            var document = result.ToString();
            document.Contains("createdUtc").ShouldBeFalse();
        }
    }
}