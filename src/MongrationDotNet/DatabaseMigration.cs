using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class DatabaseMigration : Migration
    {
        private IMongoDatabase database;
        private ILogger logger;
        public override string Type { get; } = Constants.DatabaseMigrationType;
        public ICollection<string> CollectionCreationList { get; } = new List<string>();
        public ICollection<string> CollectionDropList { get; } = new List<string>();
        public Dictionary<string, string> CollectionRenameList { get; } = new Dictionary<string, string>();

        public Dictionary<string, ICollection<Dictionary<string, SortOrder>>> CreateIndexList { get; } =
            new Dictionary<string, ICollection<Dictionary<string, SortOrder>>>();

        public Dictionary<string, Dictionary<string, int>> CreateExpiryIndexList { get; } =
            new Dictionary<string, Dictionary<string, int>>();

        public Dictionary<string, ICollection<string>> DropIndexList { get; } =
            new Dictionary<string, ICollection<string>>();

        public override async Task ExecuteAsync(IMongoDatabase mongoDatabase, ILogger logger)
        {
            this.logger = logger;
            logger?.LogInformation(LoggingEvents.DatabaseMigrationStarted, "Database migration started");
            database = mongoDatabase;

            foreach (var collectionName in CollectionCreationList)
            {
                await CreateCollection(collectionName);
            }

            foreach (var collectionName in CreateIndexList.Keys)
            {
                await CreateIndexes(collectionName,
                    CreateIndexList.GetValueOrDefault(collectionName));
            }

            foreach (var collectionName in CreateExpiryIndexList.Keys)
            {
                await CreateExpiryIndex(collectionName,
                    CreateExpiryIndexList.GetValueOrDefault(collectionName));
            }

            foreach (var collectionName in DropIndexList.Keys)
            {
                await DropIndexes(collectionName,
                    DropIndexList.GetValueOrDefault(collectionName));
            }

            foreach (var (from, to) in CollectionRenameList)
            {
                await RenameCollection(from, to);
            }

            foreach (var collectionName in CollectionDropList)
            {
                logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration, "Dropping {collectionName}",
                    collectionName);
                await database.DropCollectionAsync(collectionName);
            }

            logger?.LogInformation(LoggingEvents.DatabaseMigrationCompleted, "Database migration completed");
        }

        private async Task CreateCollection(string collectionName)
        {
            logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration, $"Creating collection {collectionName}",
                collectionName);
            var filter = new BsonDocument("name", collectionName);
            var collections = await database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            if (!await collections.AnyAsync())
            {
                await database.CreateCollectionAsync(collectionName);
            }
            else
                logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                    "Collection already exists. Skipping creating collection {collectionName}", collectionName);
        }

        private async Task RenameCollection(string from, string to)
        {
            logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                "Renaming collection from {oldCollectionName} to {newCollectionName}", from, to);
            if (!string.IsNullOrEmpty(to))
            {
                var filter = new BsonDocument("name", from);
                var collections = await database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
                if (!await collections.AnyAsync())
                {
                    logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                        "Collection does not exists. Skipping renaming collection {collectionName}", from);
                    return;
                }

                filter = new BsonDocument("name", to);
                collections = await database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
                if (!await collections.AnyAsync())
                    await database.RenameCollectionAsync(from, to);
                else
                    logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                        "Collection already exists. Skipping renaming collection {collectionName}", to);
            }
        }

        private async Task CreateIndexes(string collectionName,
            IEnumerable<IDictionary<string, SortOrder>> indexCombinations)
        {
            foreach (var indexOnFieldNames in indexCombinations)
            {
                logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                    "Creating indexes on {fields} on {collection}", string.Join(',', indexOnFieldNames.Keys),
                    collectionName);
                var indexKeys = new BsonDocument(indexOnFieldNames.Select(x => new BsonElement(x.Key, x.Value)));
                var indexName =
                    $"{collectionName}_{string.Join("-", indexOnFieldNames.Select(x => $"{x.Key}({x.Value})"))}";
                await database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKeys,
                        new CreateIndexOptions {Name = indexName}));
            }
        }

        public async Task CreateExpiryIndex(string collectionName, Dictionary<string, int> indexCombinations)
        {
            foreach (var fieldName in indexCombinations.Keys)
            {
                logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                    "Creating expiry index on {filed} on {collection}", fieldName, collectionName);
                var indexKey = new BsonDocument(fieldName, 1);
                var indexName = $"{collectionName}_{fieldName}";
                var collectionExpiryInDays = indexCombinations.GetValueOrDefault(fieldName);
                await database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKey,
                        new CreateIndexOptions
                            {Name = indexName, ExpireAfter = new TimeSpan(collectionExpiryInDays, 0, 0, 0)}));
            }
        }

        private async Task DropIndexes(string collectionName, ICollection<string> indexes)
        {
            if (!indexes.Any())
            {
                logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                    "Index list id not specified. Dropping all indexes from {collectionName}", collectionName);
                await database.GetCollection<BsonDocument>(collectionName).Indexes.DropAllAsync();
            }
            else
            {
                foreach (var index in indexes)
                {
                    logger?.LogInformation(LoggingEvents.ApplyingDatabaseMigration,
                        "Dropping index {index} from {collectionName}", index, collectionName);
                    await database.GetCollection<BsonDocument>(collectionName).Indexes.DropOneAsync(index);
                }
            }
        }
    }
}