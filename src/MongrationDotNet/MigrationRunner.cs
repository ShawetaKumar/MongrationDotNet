using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            this.migrationCollection = migrationCollection.OrderBy(x=>x.Version);
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

                if (latestAppliedMigration == null || latestAppliedMigration.Status != MigrationStatus.Completed &&
                    migration.RerunMigration)
                {
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
                else
            {
                logger?.LogInformation(LoggingEvents.MigrationSkipped,
                    "Migration has already been applied or in progress. Skipped migration for type: {type}, version: {version} and description: {description} ",
                        migration.Type, migration.Version.ToString(),
                        migration.Description);
            }

            await SetMigrationInProgress(migrationCollection);
            }
        }

        private async Task SetMigrationInProgress(IMigration migration)
        {
            if(migration.RerunMigration)
                await migrationDetailsCollection.ReplaceOneAsync(
            try
            {
                    x => x.Version == migration.Version, migration.MigrationDetails,
                await migrationDetailsCollection.InsertOneAsync(migrationCollection.MigrationDetails);
                    new ReplaceOptions { IsUpsert = true });
            else
                await migrationDetailsCollection.InsertOneAsync(migration.MigrationDetails);
            }
            catch (MongoWriteException)
            {
               await PollForMigrationStatus(migrationCollection);
            }
        }

        private async Task PollForMigrationStatus(IMigration migrationCollection)
        {
            MigrationDetails migrationDetails;
            var counter = 0;
            do
            {
                counter ++;
                await Task.Delay(5000);
                migrationDetails = await migrationDetailsCollection
                    .Find(x => x.Version == migrationCollection.Version && x.Status == "Completed")
                    .FirstOrDefaultAsync();
            } while (migrationDetails == null || counter<24);
        }

        private async Task SetMigrationAsCompleted(IMigration migration)
        {
            var migrationDetails = migration.MigrationDetails;
            migrationDetails.MarkCompleted();
            await migrationDetailsCollection.ReplaceOneAsync(
                x => x.Version == migration.Version, migrationDetails,
                new ReplaceOptions {IsUpsert = true});
        }

        private async Task SetMigrationAsErrored(IMigration migration)
        {
            var migrationDetails = migration.MigrationDetails;
            migrationDetails.MarkErrored();
            await migrationDetailsCollection.ReplaceOneAsync(
                x => x.Version == migration.Version, migrationDetails,
                new ReplaceOptions {IsUpsert = true});
        }
    }
}