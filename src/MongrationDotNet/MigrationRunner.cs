using System.Linq;
using System.Threading.Tasks;
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
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
            var migrationCollections = MigrationLocator.GetAllMigrations().OrderBy(x => x.Type).ThenBy(x => x.Version);

            foreach (var migrationCollection in migrationCollections)
            {
                var latestAppliedMigration = await migrationDetailsCollection.Find(x => x.Type == migrationCollection.Type && x.Version == migrationCollection.Version).FirstOrDefaultAsync();
                
                if (latestAppliedMigration != null) continue;
                await SetMigrationInProgress(migrationCollection);
                await migrationCollection.ExecuteAsync(database);
                await SetMigrationAsCompleted(migrationCollection);
            }
        }

        private async Task SetMigrationInProgress(Migration migrationCollection)
        {
            migrationDetails = new MigrationDetails(migrationCollection.Version, migrationCollection.Type, migrationCollection.Description);
            await migrationDetailsCollection.InsertOneAsync(migrationDetails);
        }

        private async Task SetMigrationAsCompleted(Migration migrationCollection)
        {
            migrationDetails.MarkCompleted();
            await migrationDetailsCollection.ReplaceOneAsync(x => x.Type == migrationCollection.Type && x.Version == migrationCollection.Version, migrationDetails,
                new ReplaceOptions { IsUpsert = true }); ;
        }
    }
}