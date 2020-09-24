﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    /// <summary>
    /// These are migrations performed on every document in a given collection
    /// Specify the collection name, version number (Semantic Versioning) and an optional description,
    /// Add to the MigrationFields property to create a dictionary of collection name and fields to rename/remove
    /// </summary>
    public abstract class CollectionMigration : Migration
    {
        private ILogger logger;
        public override string Type { get; } = Constants.CollectionMigrationType;

        public ICollection<(string, string)> MigrationFields { get; } =
            new List<(string, string)>();

        public virtual bool MigrateArrayValues { get; } = true;
        public abstract string CollectionName { get; }

        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            this.logger = logger;
            logger?.LogInformation(LoggingEvents.CollectionMigrationStarted, "Migration started for {collection}",
                CollectionName);
            var collection = database.GetCollection<BsonDocument>(CollectionName);
            foreach (var (from, to) in MigrationFields)
            {
                if (string.IsNullOrEmpty(to))
                    logger?.LogInformation(LoggingEvents.ApplyingCollectionMigration,
                        "Applying migration on {collection}. Removing field {field}",
                        CollectionName, from);
                else
                    logger?.LogInformation(LoggingEvents.ApplyingCollectionMigration,
                        "Applying migration on {collection}. Renaming field {from} to {to}",
                        CollectionName, from, to);

                await MigrateDocumentToNewSchema(collection, from, to);
            }

            logger?.LogInformation(LoggingEvents.CollectionMigrationCompleted, "Migration completed for {collection}",
                CollectionName);
        }

        /// <summary>
        /// This method migrate all the documents in the collection to the new schema by:
        /// 1. Rename field to the new desired name
        /// 2. Remove field from the document
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private async Task MigrateDocumentToNewSchema(IMongoCollection<BsonDocument> collection, string from, string to)
        {
            if (to.Contains("$[]"))
            {
                await RenameArrayFields(collection, from, to);
                return;
            }

            collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                !string.IsNullOrEmpty(to)
                    ? Builders<BsonDocument>.Update.Rename(from, to)
                    : Builders<BsonDocument>.Update.Unset(from));
        }

        private async Task RenameArrayFields(IMongoCollection<BsonDocument> collection, string from, string to)
        {
            if (!MigrateArrayValues)
            {
                logger?.LogInformation(LoggingEvents.ApplyingCollectionMigration,
                    "MigrateArrayValues is false. Skipping migrating array values");

                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    Builders<BsonDocument>.Update.Set(to, BsonValue.Create(null)));

                collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                    Builders<BsonDocument>.Update.Unset(from));
            }
            else
            {
                from = from.Replace("$[].", "");
                var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                var segments = from.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
                to = to.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

                foreach (var document in documents)
                {
                    document.AsBsonDocument.TryGetElement("_id", out var bsonValue);
                    RenameField(document, segments, 0, to);
                    await collection.ReplaceOneAsync(new BsonDocument("_id", bsonValue.Value), document,
                        new ReplaceOptions {IsUpsert = true});
                }
            }
        }

        private static void RenameField(BsonValue bsonValue, IReadOnlyList<string> segments, int currentSegmentIndex,
            string newFieldName)
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
            document.AsBsonDocument.Remove(from);
        }

        /// <summary>
        /// Add the property to the migration list for rename
        /// </summary>
        /// <param name="from">field name to rename</param>
        /// <param name="to">new fieldname</param>
        public void AddPropertyRename(string from, string to)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("Value cannot be null or empty.", nameof(from));
            MigrationFields.Add((from, to));
        }

        /// <summary>
        /// Add the property to the migration list to remove
        /// </summary>
        /// <param name="field">field to remove</param>
        public void AddPropertyRemoval(string field)
        {
            if (string.IsNullOrEmpty(field))
                throw new ArgumentException("Value cannot be null or empty.", nameof(field));
            MigrationFields.Add((field, string.Empty));
        }
    }
}