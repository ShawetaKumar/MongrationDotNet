using System;
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
        private readonly IEnumerable<IMigration> migrationCollection;
        private IMongoCollection<MigrationDetails> migrationDetailsCollection;

        public MigrationRunner(IMongoDatabase database, IEnumerable<IMigration> migrationCollection,
            ILogger<MigrationRunner> logger = null)
        {
            this.database = database;
            this.migrationCollection = migrationCollection;
            this.logger = logger;
        }

        public async Task Migrate()
        {
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);

            foreach (var migration in migrationCollection)
            {
                logger?.LogInformation(LoggingEvents.MigrationStarted,
                    "Migration started for type: {type}, version: {version} and description: {description} ",
                    migration.Type, migration.Version.ToString(), migration.Description);
                
                var latestAppliedMigration = await migrationDetailsCollection
                    .Find(x => x.Version == migration.Version)
                    .FirstOrDefaultAsync();

                if (latestAppliedMigration != null)
                {
                    logger?.LogInformation(LoggingEvents.MigrationSkipped,
                        "Migration has already been applied. Skipped migration for type: {type}, version: {version} and description: {description} ",
                        migration.Type, migration.Version.ToString(),
                        migration.Description);
                    continue;
                }

                try
                {
                    await SetMigrationInProgress(migration);
                    migration.Prepare();
                    await migration.ExecuteAsync(database, logger);
                    await SetMigrationAsCompleted(migration);
                    logger?.LogInformation(LoggingEvents.MigrationCompleted,
                        "Migration completed type: {type}, version: {version} and description: {description} ",
                        migration.Type, migration.Version.ToString(), migration.Description);
                }
                catch (Exception ex)
                {
                    await SetMigrationAsErrored(migration);
                    logger?.LogError(LoggingEvents.MigrationFailed,
                        "Migration failed for type: {type}, version: {version} and description: {description} with exception: {exception}. Skipping other migrations.",
                        migration.Type, migration.Version.ToString(), migration.Description, ex.Message);
                    break;
                }
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
        }

        private async Task SetMigrationAsErrored(IMigration migrationCollection)
        {
            var migrationDetails = migrationCollection.MigrationDetails;
            migrationDetails.MarkErrored();
            await migrationDetailsCollection.ReplaceOneAsync(
                x => x.Type == migrationCollection.Type && x.Version == migrationCollection.Version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });
        }
    }
}