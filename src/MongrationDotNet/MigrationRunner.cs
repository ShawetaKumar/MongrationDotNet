using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public class MigrationRunner : IMigrationRunner
    {
        private readonly IMongoDatabase database;
        private readonly ILogger<MigrationRunner> logger;
        private readonly IEnumerable<IMigration> migrationCollections;
        private IMongoCollection<MigrationDetails> migrationDetailsCollection;

        public MigrationRunner(IMongoDatabase database, IEnumerable<IMigration> migrationCollections,
            ILogger<MigrationRunner> logger = null)
        {
            this.database = database;
            this.migrationCollections = migrationCollections;
            this.logger = logger;
        }

        public async Task Migrate()
        {
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);

            foreach (var migrationCollection in migrationCollections)
            {
                logger?.LogInformation(LoggingEvents.MigrationStarted,
                    "Migration started for type: {type}, version: {version} and description: {description} ",
                    migrationCollection.Type, migrationCollection.Version.ToString(), migrationCollection.Description);
                var latestAppliedMigration = await migrationDetailsCollection
                    .Find(x => x.Type == migrationCollection.Type && x.Version == migrationCollection.Version)
                    .FirstOrDefaultAsync();

                if (latestAppliedMigration != null)
                {
                    logger?.LogInformation(LoggingEvents.MigrationSkipped,
                        "Migration has already been applied. Skipped migration for type: {type}, version: {version} and description: {description} ",
                        migrationCollection.Type, migrationCollection.Version.ToString(),
                        migrationCollection.Description);
                    continue;
                }

                await SetMigrationInProgress(migrationCollection);
                migrationCollection.Prepare();
                await migrationCollection.ExecuteAsync(database, logger);
                await SetMigrationAsCompleted(migrationCollection);
                logger?.LogInformation(LoggingEvents.MigrationCompleted,
                    "Migration completed type: {type}, version: {version} and description: {description} ",
                    migrationCollection.Type, migrationCollection.Version.ToString(), migrationCollection.Description);
            }
        }

        private async Task SetMigrationInProgress(IMigration migrationCollection)
        {
            await migrationDetailsCollection.InsertOneAsync(migrationCollection.MigrationDetails);
        }

        private async Task SetMigrationAsCompleted(IMigration migrationCollection)
        {
            var migrationDetails = migrationCollection.MigrationDetails;
            migrationDetails.MarkCompleted();
            await migrationDetailsCollection.ReplaceOneAsync(
                x => x.Type == migrationCollection.Type && x.Version == migrationCollection.Version, migrationDetails,
                new ReplaceOptions {IsUpsert = true});
            ;
        }
    }
}