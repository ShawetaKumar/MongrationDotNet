using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class CollectionMigration : Migration
    {
        public override string Type { get; } = Constants.CollectionMigrationType;
        public Dictionary<string, Dictionary<string, string>> MigrationFields { get; } =
            new Dictionary<string, Dictionary<string, string>>();
        public virtual bool MigrateArrayValues { get; } = true;

        public override async Task ExecuteAsync(IMongoDatabase database)
        {
            foreach (var collectionName in MigrationFields.Keys)
            {
                var collection = database.GetCollection<BsonDocument>(collectionName);
                await MigrateDocumentToNewSchema(collection, MigrationFields.GetValueOrDefault(collectionName));
            }
        }

        private async Task MigrateDocumentToNewSchema(IMongoCollection<BsonDocument> collection, Dictionary<string, string> elements)
        {
            /*This method migrate all the documents in the collection to the new schema by:
                1. Rename field to the new desired name
                2. Remove field from the document
            */

            foreach (var oldElement in elements.Keys)
            {
                var newElement = elements.GetValueOrDefault(oldElement);
                if (newElement.Contains("$[]"))
                {

                    await RenameArrayFields(collection, oldElement, newElement);
                    continue;
                }
                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    !string.IsNullOrEmpty(newElement)
                        ? Builders<BsonDocument>.Update.Rename(oldElement, newElement)
                        : Builders<BsonDocument>.Update.Unset(oldElement));
            }
            
        }

        private async Task RenameArrayFields(IMongoCollection<BsonDocument> collection, string from, string to)
        {
            if (!MigrateArrayValues)
            {
                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    Builders<BsonDocument>.Update.Set(to, BsonValue.Create(null)));

                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    Builders<BsonDocument>.Update.Unset(from));
            }
            else
            {
                from = from.Replace("$[].", "");
                var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                var segments = from.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries); 
                to = to.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault(); 

                foreach (var document in documents)
                {
                    document.AsBsonDocument.TryGetElement("_id", out var bsonValue);
                    RenameField(document, segments, 0, to);
                    await collection.ReplaceOneAsync(new BsonDocument("_id", bsonValue.Value), document,
                        new ReplaceOptions { IsUpsert = true });
                }
            }
        }

        private static void RenameField(BsonValue bsonValue, IReadOnlyList<string> segments, int currentSegmentIndex, string newFieldName)
        {
            while (true)
            {
                var currentSegmentName = segments[currentSegmentIndex];

                if (bsonValue.IsBsonArray)
                {
                    var array = bsonValue.AsBsonArray;
                    foreach (var arrayElement in array)
                    {
                        RenameField(arrayElement.AsBsonDocument, segments, currentSegmentIndex, newFieldName);
                    }

                    return;
                }

                var isLastNameSegment = segments.Count() == currentSegmentIndex + 1;
                if (isLastNameSegment)
                {
                    RenameField(bsonValue, currentSegmentName, newFieldName);
                    return;
                }

                var innerDocument = bsonValue.AsBsonDocument[currentSegmentName];
                bsonValue = innerDocument;
                currentSegmentIndex += 1;
            }
        }

        private static void RenameField(BsonValue document, string from, string to)
        {
            var elementFound = document.AsBsonDocument.TryGetElement(from, out var bsonValue);
            if (!elementFound) return;
            document.AsBsonDocument.Add(to, bsonValue.Value);
            document.AsBsonDocument.Remove(@from);
        }
    }
}