using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class ClientSideDocumentMigration : Migration
    {
        public override string Type { get; } = Constants.ClientSideDocumentMigrationType;
        public abstract string CollectionName { get; }
        public virtual FilterDefinition<BsonDocument> SearchFilters { get; set; } = FilterDefinition<BsonDocument>.Empty;
        public virtual string UpdateFilterField { get; } = "_id";

        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            logger?.LogInformation(LoggingEvents.DocumentMigrationStarted, "Migration started for {collection}",
                CollectionName);

            var collection = database.GetCollection<BsonDocument>(CollectionName);

            var documents = await collection.Find(SearchFilters).ToListAsync();
            foreach (var document in documents)
            {
                var migratedDocument = MigrateDocument(document);
                document.AsBsonDocument.TryGetElement(UpdateFilterField, out var bsonValue);
                await collection.ReplaceOneAsync(new BsonDocument(UpdateFilterField, bsonValue.Value), migratedDocument,
                    new ReplaceOptions { IsUpsert = true });
            }

            logger?.LogInformation(LoggingEvents.DocumentMigrationCompleted, "Migration completed for {collection}",
                CollectionName);
        }

        public abstract BsonDocument MigrateDocument(BsonDocument document);

    }
}