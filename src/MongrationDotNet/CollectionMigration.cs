using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class CollectionMigration : Migration
    {
        public override string Type { get; } = Constants.CollectionMigrationType;
        public Dictionary<string, Dictionary<string, string>> MigrationFields { get; } =
            new Dictionary<string, Dictionary<string, string>>();

        public override Task ExecuteAsync(IMongoDatabase database)
        {
            foreach (var collectionName in MigrationFields.Keys)
            {
                var collection = database.GetCollection<BsonDocument>(collectionName);
                MigrateDocumentToNewSchema(collection, MigrationFields.GetValueOrDefault(collectionName));
            }

            return Task.CompletedTask;
        }

        private static void MigrateDocumentToNewSchema(IMongoCollection<BsonDocument> collection, Dictionary<string, string> elements)
        {
            /*This method migrate all the documents in the collection to the new schema by:
                1. Rename field to the new desired name
                2. Remove field from the document
            */
            foreach (var oldElement in elements.Keys)
            {
                var newElement = elements.GetValueOrDefault(oldElement);
                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    !string.IsNullOrEmpty(newElement)
                        ? Builders<BsonDocument>.Update.Rename(oldElement, newElement)
                        : Builders<BsonDocument>.Update.Unset(oldElement));
            }
        }
    }
}