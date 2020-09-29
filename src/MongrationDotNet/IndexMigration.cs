using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    /// <summary>
    /// These are migrations performed on the database to create/drop indexes
    /// Add to the appropriate migration property to create/drop index
    /// </summary>
    public abstract class IndexMigration : Migration
    {
        private IMongoDatabase database;
        private ILogger logger;
        public override string Type { get; } = Constants.IndexMigrationType;
        public override TimeSpan ExpiryAfter { get; } = TimeSpan.FromMinutes(2);
        public abstract string CollectionName { get; }
        
        public ICollection<Dictionary<string, SortOrder>> IndexCreationList { get; } =
            new List<Dictionary<string, SortOrder>>();

        public ICollection<(string, int)> ExpiryIndexList { get; } = new List<(string, int)>();

        public ICollection<string> IndexDropList { get; } = new List<string>();

        public override async Task ExecuteAsync(IMongoDatabase mongoDatabase, ILogger logger)
        {
            this.logger = logger;
            logger?.LogInformation(LoggingEvents.IndexMigrationStarted, "Database migration started");
            database = mongoDatabase;

            await CreateIndexes();
            await CreateExpiryIndex();
            await DropIndexes();

            logger?.LogInformation(LoggingEvents.IndexMigrationCompleted, "Database migration completed");
        }

        private async Task CreateIndexes()
        {
            var collection = database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            foreach (var indexOnFieldNames in IndexCreationList)
            {
                logger?.LogInformation(LoggingEvents.ApplyingIndexMigration,
                    "Creating indexes on {fields} on {collection}", string.Join(',', indexOnFieldNames.Keys),
                    CollectionName);
                var indexKeys = new BsonDocument(indexOnFieldNames.Select(x => new BsonElement(x.Key, x.Value)));
                var indexName =
                    $"{CollectionName}_{string.Join("-", indexOnFieldNames.Select(x => $"{x.Key}({x.Value})"))}";

                if (indexes.FindIndex(i => i["name"] == indexName) < 0)
                    await collection.Indexes.CreateOneAsync(
                        new CreateIndexModel<BsonDocument>(indexKeys,
                            new CreateIndexOptions { Name = indexName }));
            }
        }

        public async Task CreateExpiryIndex()
        {
            var collection = database.GetCollection<BsonDocument>(CollectionName);
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            foreach (var (fieldName, collectionExpiryInDays) in ExpiryIndexList)
            {

                logger?.LogInformation(LoggingEvents.ApplyingIndexMigration,
                    "Creating expiry index on {field} on {collection}", fieldName, CollectionName);

                var indexKey = new BsonDocument(fieldName, 1);
                var indexName = $"{CollectionName}_{fieldName}";

                if (indexes.FindIndex(i => i["name"] == indexName) < 0)
                    await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKey,
                        new CreateIndexOptions
                            {Name = indexName, ExpireAfter = new TimeSpan(collectionExpiryInDays, 0, 0, 0)}));
            }
        }

        private async Task DropIndexes()
        {
            foreach (var index in IndexDropList)
            {
                logger?.LogInformation(LoggingEvents.ApplyingIndexMigration,
                    "Dropping index {index} from {CollectionName}", index, CollectionName);

                var collection = database.GetCollection<BsonDocument>(CollectionName);
                using var cursor = await collection.Indexes.ListAsync();
                var indexes = await cursor.ToListAsync();

                if(indexes.FindIndex(i => i["name"] == index) >= 0)
                    await collection.Indexes.DropOneAsync(index);
                else
                    logger?.LogInformation(LoggingEvents.ApplyingIndexMigration,
                        "Skipping dropping index. Index {index} not found in the collection: {CollectionName}", index, CollectionName);
            }
        }

        /// <summary>
        /// Adds to the list for creating index
        /// </summary>
        /// <param name="field">field name on which index is to be created</param>
        /// <param name="sortOrder">sort order for the index</param>
        public void AddIndex(string field, SortOrder sortOrder)
        {
            IndexCreationList.Add(new Dictionary<string, SortOrder> { { field, sortOrder } });
        }

        /// <summary>
        /// Adds to the list for creating compound index on two or more fields
        /// </summary>
        /// <param name="fields">array for fields for compound index</param>
        /// <param name="sortOrder">array for sort order for compound index</param>
        public void AddIndex(string[] fields, SortOrder[] sortOrder)
        {
            var indexCombinations = new Dictionary<string, SortOrder>();
            for (var i = 0; i < fields.Length; i++)
            {
                indexCombinations.Add(fields[i], sortOrder[i]);
            }
            IndexCreationList.Add(indexCombinations);
        }

        /// <summary>
        /// Adds to the index creation list for creating expiry index
        /// </summary>
        /// <param name="field">field name on which index is to be created</param>
        /// <param name="expiryInDays">expiry in days</param>
        public void AddExpiryIndex(string field, int expiryInDays)
        {
            ExpiryIndexList.Add((field, expiryInDays));
        }

        /// <summary>
        /// Adds to the index drop list
        /// </summary>
        /// <param name="field">field name from which index is to be dropped</param>
        public void AddToDropIndex(string field)
        {
            IndexDropList.Add(field);
        }
    }
}