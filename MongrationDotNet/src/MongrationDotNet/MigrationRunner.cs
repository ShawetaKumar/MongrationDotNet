using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public class MigrationRunner
    {
        private readonly IMongoDatabase database;
        private IMongoCollection<MigrationDetails> migrationDetailsCollection;
        private MigrationDetails migrationDetails;

        public MigrationRunner(IMongoDatabase database)
        {
            this.database = database;
        }
        public async Task Migrate()
        {
            migrationDetails = new MigrationDetails();
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(typeof(MigrationDetails).Name);
            await ApplyDocumentMigration();
            await ApplyCollectionMigration();
        }

        private async Task ApplyDocumentMigration()
        {
            var result = await migrationDetailsCollection.Find(x => x.Type == Constants.CollectionMigrationType).SortByDescending(x => x.Version).FirstOrDefaultAsync();

            var migrationCollections = result == null ? MigrationLocator.GetAllDocumentMigrations() : MigrationLocator.GetAllDocumentMigrations().Where(x => x.Version > result.Version);

            foreach (var migrationCollection in migrationCollections)
            {
                foreach (var collectionName in migrationCollection.MigrationObjects.Keys)
                {
                    var collection = database.GetCollection<BsonDocument>(collectionName);
                    UpdateDocument(collection, migrationCollection.MigrationObjects.GetValueOrDefault(collectionName));

                }
                migrationDetails.SetMigrationDetails(migrationCollection.Version, migrationCollection.Type, migrationCollection.Description);
                await migrationDetailsCollection.InsertOneAsync(migrationDetails);
            }
        }

        private async Task ApplyCollectionMigration()
        {
            var result = await migrationDetailsCollection.Find(x => x.Type == Constants.DatabaseMigrationType).SortByDescending(x => x.Version).FirstOrDefaultAsync();

            var migrationCollections = result == null ? MigrationLocator.GetAllCollectionMigrations() : MigrationLocator.GetAllCollectionMigrations().Where(x => x.Version > result.Version);

            foreach (var migrationCollection in migrationCollections)
            {
                foreach (var collectionName in migrationCollection.CollectionCreationList)
                {
                    await CreateCollection(collectionName);
                }

                foreach (var collectionName in migrationCollection.CreateIndexList.Keys)
                {
                    await CreateIndexes(collectionName,
                        migrationCollection.CreateIndexList.GetValueOrDefault(collectionName));
                }

                foreach (var collectionName in migrationCollection.CreateExpiryIndexList.Keys)
                {
                    await CreateExpiryIndex(collectionName,
                        migrationCollection.CreateExpiryIndexList.GetValueOrDefault(collectionName));
                }

                foreach (var collectionName in migrationCollection.DropIndexList.Keys)
                {
                    await DropIndexes(collectionName,
                        migrationCollection.DropIndexList.GetValueOrDefault(collectionName));
                }

                foreach (var (from, to) in migrationCollection.CollectionRenameList)
                {
                    await database.RenameCollectionAsync(from, to);
                }

                foreach (var collectionName in migrationCollection.CollectionDropList)
                {
                    await database.DropCollectionAsync(collectionName);
                }

                migrationDetails.SetMigrationDetails(migrationCollection.Version, migrationCollection.Type, migrationCollection.Description);
                await migrationDetailsCollection.InsertOneAsync(migrationDetails);
            }
        }

        private static void UpdateDocument(IMongoCollection<BsonDocument> collection, Dictionary<string, string> elements)
        {
            foreach (var oldElement in elements.Keys)
            {
                var newElement = elements.GetValueOrDefault(oldElement);
                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    !string.IsNullOrEmpty(newElement)
                        ? Builders<BsonDocument>.Update.Rename(oldElement, newElement)
                        : Builders<BsonDocument>.Update.Unset(oldElement));
            }
        }

        private async Task CreateCollection(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            if (!await collections.AnyAsync())
            {
                await database.CreateCollectionAsync(collectionName);
            }
        }

        private async Task RenameCollection(string from, string to)
        {
            if (!string.IsNullOrEmpty(to))
            {
                var filter = new BsonDocument("name", to);
                var collections = await database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
                if (!await collections.AnyAsync())
                {
                    await database.RenameCollectionAsync(from, to);
                }
            }
        }

        private async Task CreateIndexes(string collectionName, IEnumerable<IDictionary<string, SortOrder>> indexCombinations)
        {
            foreach (var indexOnFieldNames in indexCombinations)
            {
                var indexKeys = new BsonDocument(indexOnFieldNames.Select(x => new BsonElement(x.Key, x.Value)));
                var indexName =
                    $"{collectionName}_{string.Join("-", indexOnFieldNames.Select(x => $"{x.Key}({x.Value})"))}";
                await database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKeys,
                        new CreateIndexOptions { Name = indexName }));
            }
        }

        public async Task CreateExpiryIndex(string collectionName, Dictionary<string, int> indexCombinations)
        {
            foreach (var fieldName in indexCombinations.Keys)
            {
                var indexKey = new BsonDocument(fieldName, 1);
                var indexName = $"{collectionName}_{fieldName}";
                var collectionExpiryInDays = indexCombinations.GetValueOrDefault(fieldName);
                await database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKey,
                        new CreateIndexOptions { Name = indexName, ExpireAfter = new TimeSpan(collectionExpiryInDays, 0, 0, 0) }));
            }
        }

        private async Task DropIndexes(string collectionName, ICollection<string> indexes)
        {
            if (!indexes.Any())
                await database.GetCollection<BsonDocument>(collectionName).Indexes.DropAllAsync();
            else
            {
                foreach (var index in indexes)
                {
                    await database.GetCollection<BsonDocument>(collectionName).Indexes.DropOneAsync(index);
                }
            }
        }
    }
}