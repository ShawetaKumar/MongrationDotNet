using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongrationDotNet.Tests
{
    public class IndexMigrationTests : TestBase
    {
        [SetUp]
        public async Task SetupDatabase()
        {
            await Database.CreateCollectionAsync(TestBase.CollectionName);
        }

        [TearDown]
        public async Task Reset()
        {
            await ResetMigrationDetails();
        }

        [Test]
        public async Task Migration_ShouldCreateIndexes_WhenCreateIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();

            var collection = Database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            indexes.Should().Contain(index => index["name"] == $"{TestBase.CollectionName}_name({SortOrder.Ascending})");
            indexes.Should().Contain(index => index["name"] == $"{TestBase.CollectionName}_status({SortOrder.Descending})");
        }

        [Test]
        public async Task Migration_ShouldCreateCompoundIndexes_WhenCreateIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();

            var collection = Database.GetCollection<BsonDocument>(TestBase.CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            indexes.Should().Contain(index =>
                index["name"] == $"{TestBase.CollectionName}_lastUpdatedUtc(Ascending)-_id(Ascending)");
            indexes.Should().Contain(index =>
                index["name"] == $"{TestBase.CollectionName}__id(Ascending)-lastUpdatedUtc(Ascending)");
        }

        [Test]
        public async Task Migration_ShouldCreateIndexesOnEmbeddedField_WhenCreateIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();

            var collection = Database.GetCollection<BsonDocument>(TestBase.CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            indexes.Should().Contain(index => index["name"] == $"{TestBase.CollectionName}_store.id({SortOrder.Ascending})");
        }

        [Test]
        public async Task Migration_ShouldCreateExpiryIndex_WhenCreateExpiryIndexListContainsIndexNames()
        {
            await MigrationRunner.Migrate();

            var collection = Database.GetCollection<BsonDocument>(TestBase.CollectionName);
            using var cursor = await collection.Indexes.ListAsync();

            var indexes = await cursor.ToListAsync();
            indexes.Should().Contain(index => index["name"] == $"{TestBase.CollectionName}_lastUpdatedUtc");
        }

        [Test]
        public async Task Migration_ShouldDropIndexes_WhenDropIndexListContainsIndexNames()
        {
            const string collectionName = "indexCollection";
            await Database.CreateCollectionAsync(collectionName);
            var indexList = new Dictionary<string, SortOrder>
            {
                {"name", SortOrder.Ascending},
                {"status", SortOrder.Descending},
                {"lastUpdatedUtc", SortOrder.Descending}
            };
            await CreateIndexOnFields(collectionName, indexList);
            var collection = Database.GetCollection<BsonDocument>(collectionName);

            await MigrationRunner.Migrate();

            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            indexes.Should().Contain(index => index["name"] == $"{collectionName}_lastUpdatedUtc");
            indexes.Should().NotContain(index => index["name"] == $"{collectionName}_name");
            indexes.Should().NotContain(index => index["name"] == $"{collectionName}_status");
        }

        private async Task CreateIndexOnFields(string collectionName, Dictionary<string, SortOrder> indexCombinations)
        {
            foreach (var indexOnFieldNames in indexCombinations.Keys)
            {
                var indexKeys = new BsonDocument(indexOnFieldNames,
                    indexCombinations.GetValueOrDefault(indexOnFieldNames));
                var indexName =
                    $"{collectionName}_{indexOnFieldNames}";
                await Database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKeys,
                        new CreateIndexOptions {Name = indexName}));
            }
        }
    }
}