﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class ClientSideDocumentMigration : Migration
    {
        public override string Type { get; } = Constants.ClientSideDocumentMigrationType;
        public abstract string CollectionName { get; }

        public virtual FilterDefinition<BsonDocument> SearchFilters { get; set; } =
            FilterDefinition<BsonDocument>.Empty;

        public virtual string UpdateFilterField { get; } = "_id";
        public virtual int BatchSize { get; } = int.MaxValue;

        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            logger?.LogInformation(LoggingEvents.DocumentMigrationStarted, "Migration started for {collection}",
                CollectionName);

            var collection = database.GetCollection<BsonDocument>(CollectionName);

            var updated = 0;
            IEnumerable<BsonDocument> documents;
            do
            {
                documents = (await collection.Find(SearchFilters).ToListAsync()).Skip(updated).Take(BatchSize);
                foreach (var document in documents)
                {
                    var migratedDocument = MigrateDocument(document);
                    document.AsBsonDocument.TryGetElement(UpdateFilterField, out var bsonValue);
                    await collection.ReplaceOneAsync(new BsonDocument(UpdateFilterField, bsonValue.Value),
                        migratedDocument,
                        new ReplaceOptions {IsUpsert = true});
                    updated++;
                }
            } while (documents.Any());

            logger?.LogInformation(LoggingEvents.DocumentMigrationCompleted, "Migration completed for {collection}",
                CollectionName);
        }

        public abstract BsonDocument MigrateDocument(BsonDocument document);
    }
}