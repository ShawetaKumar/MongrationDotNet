using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public class MigrationRunner : IMigrationRunner
    {
        private readonly IMongoDatabase database;
        private readonly MigrationOptions migrationOptions;
        private readonly ILogger<MigrationRunner> logger;
        private readonly IEnumerable<IMigration> migrationCollection;
        private IMongoCollection<MigrationDetails> migrationDetailsCollection;

        public MigrationRunner(IMongoDatabase database, IEnumerable<IMigration> migrationCollection, IOptions<MigrationOptions> options,
            ILogger<MigrationRunner> logger = null)
        {
            this.database = database;
            this.migrationOptions = options.Value;
            this.migrationCollection = migrationCollection.OrderBy(x=>x.Version);
            this.logger = logger;
        }

        public async Task<List<MigrationDetails>> Migrate()
        {
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
            var migrationsApplied = new List<MigrationDetails>();
            try
            {
                if (await AnyMigrationInProgress())
                {
                    await PollForMigrationStatus();
                }

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
                        MigrationDetails migrationApplied = null;
                        try
                        {
                            var success = await SetMigrationInProgress(migration);
                            if (!success)
                                continue;
                            
                            migration.Prepare();
                            await migration.ExecuteAsync(database, logger);
                            migrationApplied = await SetMigrationAsCompleted(migration);
                            logger?.LogInformation(LoggingEvents.MigrationCompleted,
                                "Migration completed type: {type}, version: {version} and description: {description} ",
                                migration.Type, migration.Version.ToString(), migration.Description);
                        }
                        catch (TimeoutException ex)
                        {
                            logger?.LogError(LoggingEvents.MigrationFailed,
                                "Migration failed for type: {type}, version: {version} and description: {description} with exception: {exception}.",
                                migration.Type, migration.Version.ToString(), migration.Description, ex.Message);
                        }
                        catch (Exception ex)
                        {
                            migrationApplied = await SetMigrationAsErrored(migration, ex.Message);
                            logger?.LogError(LoggingEvents.MigrationFailed,
                                "Migration failed for type: {type}, version: {version} and description: {description} with exception: {exception}. Skipping other migrations.",
                                migration.Type, migration.Version.ToString(), migration.Description, ex.Message);
                            break;
                        }
                        finally
                        {
                            if(migrationApplied != null)
                                migrationsApplied.Add(migrationApplied);
                        }
                    }
                    else
                    {
                        logger?.LogInformation(LoggingEvents.MigrationSkipped,
                            "Migration has already been applied or in progress. Skipped migration for type: {type}, version: {version} and description: {description} ",
                            migration.Type, migration.Version.ToString(),
                            migration.Description);
                    }   
                }

                return migrationsApplied;
            }
            catch (Exception ex)
            {
                logger?.LogError(LoggingEvents.MigrationFailed,
                    "Migration failed with exception: {exception}", ex.Message);
                throw;
            }
        }

        private async Task<bool> AnyMigrationInProgress()
        {
            var count = await migrationDetailsCollection
                .CountDocumentsAsync(x => x.Status == MigrationStatus.InProgress);
            return count > 0;
        }

        private async Task<bool> SetMigrationInProgress(IMigration migration)
        {
            try
            {
                if (migration.RerunMigration)
                {
                    DeleteResult result = null;
                    var previousRun = await migrationDetailsCollection.Find(x => x.Version == migration.Version).SingleOrDefaultAsync();
                    if(previousRun != null)
                        result = await migrationDetailsCollection.DeleteOneAsync(x => x.Version == migration.Version);
                    if(previousRun == null || result?.DeletedCount == 1)
                        await migrationDetailsCollection.InsertOneAsync(migration.MigrationDetails);
                }
                else
                    await migrationDetailsCollection.InsertOneAsync(migration.MigrationDetails);
                return true;
            }
            catch (MongoWriteException)
            {
                await PollForMigrationStatus();
                return false;
            }
            
        }

        private async Task<MigrationDetails> SetMigrationAsCompleted(IMigration migration)
        {
            var migrationDetails = migration.MigrationDetails;
            migrationDetails.MarkCompleted();
            await migrationDetailsCollection.ReplaceOneAsync(
                x => x.Version == migration.Version, migrationDetails,
                new ReplaceOptions {IsUpsert = false});
            return migrationDetails;
        }

        private async Task<MigrationDetails> SetMigrationAsErrored(IMigration migration, string errorMessage)
        {
            var migrationDetails = migration.MigrationDetails;
            migrationDetails.MarkErrored(errorMessage);
            await migrationDetailsCollection.ReplaceOneAsync(
                x => x.Version == migration.Version, migrationDetails,
                new ReplaceOptions {IsUpsert = false});
            return migrationDetails;
        }

        private async Task PollForMigrationStatus()
        {
            var pollingInterval = TimeSpan.FromMilliseconds(migrationOptions.MigrationProgressDbPollingInterval);
            var pollingTimeout = TimeSpan.FromMilliseconds(migrationOptions.MigrationProgressDbPollingTimeout);
            bool migrationInProgress;
            var start = DateTime.UtcNow;
            do
            {
                await Task.Delay(pollingInterval);
                migrationInProgress = await AnyMigrationInProgress();
            } while (migrationInProgress && DateTime.UtcNow - start < pollingTimeout);

            if (migrationInProgress && DateTime.UtcNow - start > pollingTimeout)
                throw new TimeoutException("Timing out on waiting for the Migration running on other thread");
        }
    }
}