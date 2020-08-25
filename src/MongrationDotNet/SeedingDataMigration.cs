using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class SeedingDataMigration : Migration
    {
        public override string Type { get; } = Constants.SeedingDataMigrationType;

        public abstract string CollectionName { get; }

        public ICollection<BsonDocument> Seeds { get; } =
            new List<BsonDocument>();

        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            logger?.LogInformation(LoggingEvents.SeedingDataMigrationStarted, "Migration started for {collection}",
                CollectionName);

            var collection = database.GetCollection<BsonDocument>(CollectionName);

            var documentsCount = await collection.Find(FilterDefinition<BsonDocument>.Empty).CountDocumentsAsync();

            if (documentsCount > 0)
            {
                logger?.LogInformation("Collection already contains {documentsCount} documents. Seeding Data Migration skipped for {collection}",
                    documentsCount, CollectionName);
                return;
            }

            await collection.InsertManyAsync(Seeds);

            logger?.LogInformation(LoggingEvents.SeedingDataMigrationCompleted, "Migration completed for {collection}",
                CollectionName);
        }
    }
}