using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    /// <summary>
    /// This migration can be used if the calculation of the new field value is required to be done at client end
    /// This migration is applied by looping through each of the document in the collection based on Filter specified
    /// </summary>
    public abstract class ClientSideDocumentMigration : Migration
    {
        public override string Type { get; } = Constants.ClientSideDocumentMigrationType;
        public abstract string CollectionName { get; }
        public virtual FilterDefinition<BsonDocument> SearchFilters { get; set; } =
            FilterDefinition<BsonDocument>.Empty;
        public virtual string UpdateFilterField { get; } = "_id";
        public virtual int BatchSize { get; } = int.MaxValue;
        public virtual bool PageThroughAllFilteredDocuments { get; } = true;

        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            logger?.LogInformation(LoggingEvents.DocumentMigrationStarted, "Migration started for {collection}",
                CollectionName);

            var collection = database.GetCollection<BsonDocument>(CollectionName);

            var updated = 0;
            IEnumerable<BsonDocument> documents;
            do
            {
                documents = PageThroughAllFilteredDocuments 
                    ? (await collection.Find(SearchFilters).Skip(updated).Limit(BatchSize).ToListAsync()).ToArray() 
                    : (await collection.Find(SearchFilters).Limit(BatchSize).ToListAsync()).ToArray();
                
                foreach (var document in documents)
                {
                    var migratedDocument = MigrateDocument(document);
                    document.AsBsonDocument.TryGetElement(UpdateFilterField, out var bsonValue);
                    await collection.ReplaceOneAsync(new BsonDocument(UpdateFilterField, bsonValue.Value),
                        migratedDocument,
                        new ReplaceOptions {IsUpsert = false});
                    updated++;
                }
            } while (documents.Any());

            logger?.LogInformation(LoggingEvents.DocumentMigrationCompleted, "Migration completed for {collection}. {documentsUpdated} documents updated",
                CollectionName, updated);
        }

        /// <summary>
        /// override this method to restructure the document.
        /// The returned restructured document is then replaced in the collection
        /// </summary>
        /// <param name="document">document to be migrated to new schema</param>
        /// <returns></returns>
        public abstract BsonDocument MigrateDocument(BsonDocument document);
    }
}