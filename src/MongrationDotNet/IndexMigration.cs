﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class IndexMigration : Migration
    {
        private IMongoDatabase database;
        private ILogger logger;
        public override string Type { get; } = Constants.IndexMigrationType;
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
            foreach (var indexOnFieldNames in IndexCreationList)
            {
                logger?.LogInformation(LoggingEvents.ApplyingIndexMigration,
                    "Creating indexes on {fields} on {collection}", string.Join(',', indexOnFieldNames.Keys),
                    CollectionName);
                var indexKeys = new BsonDocument(indexOnFieldNames.Select(x => new BsonElement(x.Key, x.Value)));
                var indexName =
                    $"{CollectionName}_{string.Join("-", indexOnFieldNames.Select(x => $"{x.Key}({x.Value})"))}";
                await database.GetCollection<BsonDocument>(CollectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKeys,
                        new CreateIndexOptions { Name = indexName }));
            }
        }

        public async Task CreateExpiryIndex()
        {
            foreach (var (fieldName, collectionExpiryInDays) in ExpiryIndexList)
            {

                logger?.LogInformation(LoggingEvents.ApplyingIndexMigration,
                    "Creating expiry index on {field} on {collection}", fieldName, CollectionName);

                var indexKey = new BsonDocument(fieldName, 1);
                var indexName = $"{CollectionName}_{fieldName}";
                await database.GetCollection<BsonDocument>(CollectionName).Indexes.CreateOneAsync(
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

        public void AddIndex(string field, SortOrder sortOrder)
        {
            IndexCreationList.Add(new Dictionary<string, SortOrder> { { field, sortOrder } });
        }

        public void AddIndex(string[] fields, SortOrder[] sortOrder)
        {
            var indexCombinations = new Dictionary<string, SortOrder>();
            for (var i = 0; i < fields.Length; i++)
            {
                indexCombinations.Add(fields[i], sortOrder[i]);
            }
            IndexCreationList.Add(indexCombinations);
        }

        public void AddExpiryIndex(string field, int expiryInDays)
        {
            ExpiryIndexList.Add((field, expiryInDays));
        }

        public void AddToDropIndex(string field)
        {
            IndexDropList.Add(field);
        }
    }
}