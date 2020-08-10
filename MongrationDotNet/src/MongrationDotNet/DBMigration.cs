using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public class DBMigration
    {
        private IMongoDatabase Database;

        public DBMigration(IMongoDatabase database)
        {
            Database = database;
        }
        public async Task Migrate()
        {
            var migrationDetails = new MigrationDetails();
            var migrationDetailsCollection = Database.GetCollection<MigrationDetails>(typeof(MigrationDetails).Name);
            var result = await migrationDetailsCollection.Find(FilterDefinition<MigrationDetails>.Empty).SortByDescending(x => x.Version).FirstOrDefaultAsync();

            var migrationCollections = result == null ? MigrationLocator.GetAllMigrations() : MigrationLocator.GetAllMigrations().Where(x => x.Version > result.Version);

            foreach (var migrationCollection in migrationCollections)
            {
                foreach (var collectionName in migrationCollection.MigrationObjects.Keys)
                {
                    var collection = Database.GetCollection<BsonDocument>(collectionName);
                    Update(collection, migrationCollection.MigrationObjects.GetValueOrDefault(collectionName));

                }
                migrationDetails.SetMigrationDetails(migrationCollection.Version, migrationCollection.Description);
                await migrationDetailsCollection.InsertOneAsync(migrationDetails);
            }
        }

        public static void Update(IMongoCollection<BsonDocument> collection, Dictionary<string, string> elements)
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