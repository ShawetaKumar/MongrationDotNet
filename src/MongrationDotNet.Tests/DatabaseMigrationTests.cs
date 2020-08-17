using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class DatabaseMigrationTests : TestBase
    {
        private Version Version => new Version(1, 1, 1, 0);
        private IMongoCollection<MigrationDetails> migrationCollection;

        [SetUp]
        public async Task SetupDatabase()
        {
            await Database.CreateCollectionAsync(CollectionName);
            migrationCollection = Database.GetCollection<MigrationDetails>(typeof(MigrationDetails).Name);
        }

        [TearDown]
        public async Task ResetMigrationDetails()
        {
            await Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
        }

        [Test]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();
            
            var result = await migrationCollection.Find(x => x.Type == Constants.DatabaseMigrationType)
                .FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(Version);
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyInProgress()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.DatabaseMigrationType, "database migration");
            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();
            
            var result = await migrationCollection
                .Find(x => x.Version == Version && x.Type == Constants.DatabaseMigrationType).ToListAsync();
          
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyApplied()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.DatabaseMigrationType, "database migration");
            migrationDetails.MarkCompleted();
            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();
            
            var result = await migrationCollection
                .Find(x => x.Version == Version && x.Type == Constants.DatabaseMigrationType).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldCreateCollection_WhenCreationListContainsCollectionNames()
        {
            await MigrationRunner.Migrate();
            
            var filter = new BsonDocument("name", CollectionName);
            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
           
            var exits = await collections.AnyAsync();
            exits.ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldDropCollection_WhenDropListContainsCollectionNames()
        {
            const string collectionName = "myCollection";
            await Database.CreateCollectionAsync(collectionName);
            await MigrationRunner.Migrate();
            var filter = new BsonDocument("name", collectionName);
            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            
            var exists = await collections.AnyAsync();
            exists.ShouldBeFalse();
        }

        [Test]
        public async Task Migration_ShouldRenameCollection_WhenRenameListContainsCollectionNames()
        {
            const string oldCollection = "oldCollection";
            const string newCollection = "newCollection";
            await Database.CreateCollectionAsync(oldCollection);
            
            await MigrationRunner.Migrate();
            
            var filter = new BsonDocument("name", oldCollection);
            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            var exists = await collections.AnyAsync();
            
            exists.ShouldBeFalse();

            filter = new BsonDocument("name", newCollection);
            collections = await Database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            exists = await collections.AnyAsync();

            exists.ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldExecuteSuccessfullyAndNotThrowError_WhenDropListContainsANonExistingCollection()
        {
            var version = new Version(1, 1, 1, 1);
            await MigrationRunner.Migrate();
            
            var result = await migrationCollection.Find(x => x.Version == version && x.Type == Constants.DatabaseMigrationType).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(version);
        }

        [Test]
        public async Task Migration_ShouldCreateIndexes_WhenCreateIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();
            
            var collection = Database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
           
            indexes.Should().Contain(index => index["name"] == $"{CollectionName}_name({SortOrder.Ascending})");
            indexes.Should().Contain(index => index["name"] == $"{CollectionName}_status({SortOrder.Descending})");
        }

        [Test]
        public async Task Migration_ShouldCreateCompoundIndexes_WhenCreateIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();
            
            var collection = Database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
            
            indexes.Should().Contain(index => index["name"] == $"{CollectionName}_lastUpdatedUtc(Ascending)-_id(Ascending)");
            indexes.Should().Contain(index => index["name"] == $"{CollectionName}__id(Ascending)-lastUpdatedUtc(Ascending)");
        }

        [Test]
        public async Task Migration_ShouldCreateIndexesOnEmbeddedField_WhenCreateIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();
            
            var collection = Database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
            
            indexes.Should().Contain(index => index["name"] == $"{CollectionName}_store.id({SortOrder.Ascending})");
        }

        [Test]
        public async Task Migration_ShouldCreateExpiryIndex_WhenCreateExpiryIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();
            
            var collection = Database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            
            var indexes = await cursor.ToListAsync();
            indexes.Should().Contain(index => index["name"] == $"{CollectionName}_lastUpdatedUtc");
        }

        [Test]
        public async Task Migration_ShouldDropIndexes_WhenDropIndexListContainsIndexNames()
        {
            const string collectionName = "newCollection1";
            await Database.CreateCollectionAsync(collectionName);
            var indexList = new Dictionary<string, SortOrder> { { "name", SortOrder.Ascending }, { "status", SortOrder.Descending }, { "lastUpdatedUtc", SortOrder.Descending } };
            await CreateIndexOnFields(collectionName, indexList);
            var collection = Database.GetCollection<BsonDocument>(collectionName);
            
            await MigrationRunner.Migrate();
            
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
            
            indexes.Should().Contain(index => index["name"] == $"{collectionName}_lastUpdatedUtc");
            indexes.Should().NotContain(index => index["name"] == $"{collectionName}_name");
            indexes.Should().NotContain(index => index["name"] == $"{collectionName}_status");
        }

        [Test]
        public async Task Migration_ShouldDropAllIndexes_WhenDropIndexListDoesNotContainsIndexNames()
        {
            const string collectionName = "newCollection2";
            await Database.CreateCollectionAsync(collectionName);
            var indexList = new Dictionary<string, SortOrder> { { "type", SortOrder.Ascending }, { "createdUtc", SortOrder.Descending } };
            await CreateIndexOnFields(collectionName, indexList);
            var collection = Database.GetCollection<BsonDocument>(collectionName);
            
            await MigrationRunner.Migrate();
            
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
            
            indexes.Should().Contain(index => index["name"] == "_id_");
            indexes.Should().NotContain(index => index["name"] == $"{collectionName}_type");
            indexes.Should().NotContain(index => index["name"] == $"{collectionName}_createdUtc");
        }

        private async Task CreateIndexOnFields(string collectionName, Dictionary<string, SortOrder> indexCombinations)
        {
            foreach (var indexOnFieldNames in indexCombinations.Keys)
            {
                var indexKeys = new BsonDocument(indexOnFieldNames, indexCombinations.GetValueOrDefault(indexOnFieldNames));
                var indexName =
                    $"{collectionName}_{indexOnFieldNames}";
                await Database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKeys,
                        new CreateIndexOptions { Name = indexName }));
            }
        }
    }
}