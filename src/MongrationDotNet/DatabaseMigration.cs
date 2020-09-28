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
    /// These are migrations performed on the database to create/rename/drop collection
    /// Add to the appropriate migration property to create/rename/drop
    /// Only add to that list which you need to migrate
    /// </summary>
    public abstract class DatabaseMigration : Migration
    {
        private IMongoDatabase database;
        private ILogger logger;
        public override string Type { get; } = Constants.DatabaseMigrationType;
        public override TimeSpan ExpiryAfter { get; } = TimeSpan.FromMinutes(2);
        public ICollection<string> CollectionCreationList { get; } = new List<string>();
        public ICollection<string> CollectionDropList { get; } = new List<string>();
        public Dictionary<string, string> CollectionRenameList { get; } = new Dictionary<string, string>();

        public override async Task ExecuteAsync(IMongoDatabase mongoDatabase, ILogger logger)
        {
            this.logger = logger;
            logger?.LogInformation(LoggingEvents.DatabaseMigrationStarted, "Database migration started");
            database = mongoDatabase;

            foreach (var collectionName in CollectionCreationList)
            {
                await CreateCollection(collectionName);
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

        /// <summary>
        /// Adds to the collection creation list
        /// </summary>
        /// <param name="collectionName">collection name to be created</param>
        public void AddCollectionToCreate(string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(collectionName));
            
            if (!CollectionCreationList.Contains(collectionName))
                CollectionCreationList.Add(collectionName);
        }

        /// <summary>
        /// Adds to the collection drop list
        /// </summary>
        /// <param name="collectionName">collection name to be dropped</param>
        public void AddCollectionToDrop(string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(collectionName));

            if (!CollectionDropList.Contains(collectionName))
                CollectionDropList.Add(collectionName);
        }

        /// <summary>
        /// Adds to the collection rename list
        /// </summary>
        /// <param name="from">collection name to be renamed</param>
        /// <param name="to">new name for the collection</param>
        public void AddCollectionForRename(string from, string to)
        {
            if (!CollectionRenameList.ContainsKey(from))
                CollectionRenameList.Add(from, to);
        }
    }
}