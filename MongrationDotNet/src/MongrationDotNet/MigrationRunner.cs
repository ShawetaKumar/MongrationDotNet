using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public class MigrationRunner
    {
        private readonly IMongoDatabase database;
        private IMongoCollection<MigrationDetails> migrationDetailsCollection;
        private MigrationDetails migrationDetails;

        public MigrationRunner(IMongoDatabase database)
        {
            this.database = database;
        }
        public async Task Migrate()
        {
            migrationDetails = new MigrationDetails();
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(typeof(MigrationDetails).Name);
            await ApplyDocumentMigration();
        }

        private async Task ApplyDocumentMigration()
        {
            var result = await migrationDetailsCollection.Find(x => x.Type == Constants.DocumentMigrationType).SortByDescending(x => x.Version).FirstOrDefaultAsync();

            var migrationCollections = result == null ? MigrationLocator.GetAllDocumentMigrations() : MigrationLocator.GetAllDocumentMigrations().Where(x => x.Version > result.Version);

            foreach (var migrationCollection in migrationCollections)
            {
                foreach (var collectionName in migrationCollection.MigrationObjects.Keys)
                {
                    var collection = database.GetCollection<BsonDocument>(collectionName);
                    UpdateDocument(collection, migrationCollection.MigrationObjects.GetValueOrDefault(collectionName));

                }
                migrationDetails.SetMigrationDetails(migrationCollection.Version, migrationCollection.Type, migrationCollection.Description);
                await migrationDetailsCollection.InsertOneAsync(migrationDetails);
            }
        }

        private static void UpdateDocument(IMongoCollection<BsonDocument> collection, Dictionary<string, string> elements)
        {
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