using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongrationDotNet
{
    /// <summary>
    /// These are migrations performed on the collection to upload a document to the collection
    /// Add the bson documents to be uploaded to Seeds property
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public abstract class SeedingDataMigration<TDocument> : Migration
    {
        public override string Type { get; } = Constants.SeedingDataMigrationType;

        public abstract string CollectionName { get; }

        public ICollection<TDocument> Seeds { get; } =
            new List<TDocument>();

        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            logger?.LogInformation(LoggingEvents.SeedingDataMigrationStarted, "Migration started for {collection}",
                CollectionName);

            var collection = database.GetCollection<TDocument>(CollectionName);
            await collection.InsertManyAsync(Seeds);

            logger?.LogInformation(LoggingEvents.SeedingDataMigrationCompleted, "Migration completed for {collection}",
                CollectionName);
        }

        /// <summary>
        /// Adds to the Seeds property for document to be uploaded
        /// </summary>
        /// <param name="seed">Document to be uploaded</param>
        public void Seed(TDocument seed)
        {
            Seeds.Add(seed);
        }
    }
}