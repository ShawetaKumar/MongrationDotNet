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

      /// <summary>
      /// Method which executes the whole migration process
      /// </summary>
      /// <returns>List of all Migrations applied. Migrations which gets skipped are excluded</returns>
        public async Task<List<MigrationDetails>> Migrate()
        {
            migrationDetailsCollection = database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
            var migrationsApplied = new List<MigrationDetails>();
            try
            {
                foreach (var migration in migrationCollection)
                {
                    logger?.LogInformation(LoggingEvents.MigrationStarted,
                        "Migration started for type: {type}, version: {version} and description: {description} ",
                        migration.Type, migration.Version.ToString(), migration.Description);

                    var latestAppliedMigration = await migrationDetailsCollection
                        .Find(x => x.Version == migration.Version)
                        .FirstOrDefaultAsync();

                    if (latestAppliedMigration == null || latestAppliedMigration.Status != MigrationStatus.Completed &&
                        migration.RerunMigration && migration.MigrationDetails.ExpireAt > DateTime.UtcNow)
                    {
                        MigrationDetails migrationApplied = null;
                        try
                        {
                            var success = await SetMigrationInProgress(migration);
                            //not able to set current migration in progress. Not safe to run the subsequent migrations
                            if (!success)
                            {
                                logger?.LogError(LoggingEvents.MigrationFailed,
                                    "Failed to set migration in progress for type: {type}, version: {version} and description: {description}. Skipping other migrations.",
                                    migration.Type, migration.Version.ToString(), migration.Description);
                                break;
                            }

                            migration.Prepare();
                            await migration.ExecuteAsync(database, logger);
                            migrationApplied = await SetMigrationAsCompleted(migration);
                            logger?.LogInformation(LoggingEvents.MigrationCompleted,
                                "Migration completed type: {type}, version: {version} and description: {description} ",
                                migration.Type, migration.Version.ToString(), migration.Description);
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
                    else if (latestAppliedMigration.Status == MigrationStatus.InProgress)
                    {
                        logger?.LogInformation(LoggingEvents.MigrationSkipped,
                            "Migration has already been started by another node. Skipping migration on current node");
                        break;
                    }
                    else
                    {
                        logger?.LogInformation(LoggingEvents.MigrationSkipped,
                            "Migration has already been applied. Skipped migration for type: {type}, version: {version} and description: {description} ",
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

       /// <summary>
       /// Sets migration is progress
       /// If any other node is running the migration the calling node will poll till migration is completed or till timeout 
       /// </summary>
       /// <param name="migration">Migration to start</param>
       /// <returns></returns>
        private async Task<bool> SetMigrationInProgress(IMigration migration)
        {
            try
            {
                var setToProgressSuccessfully = false;
                //if migration is set to rerun after previous run
                if (migration.RerunMigration)
                {
                    DeleteResult result = null;
                    var previousRun = await migrationDetailsCollection.Find(x => x.Version == migration.Version).SingleOrDefaultAsync();
                    //delete the previous run if exits
                    if(previousRun != null && previousRun.Status == MigrationStatus.Errored)
                        result = await migrationDetailsCollection.DeleteOneAsync(x => x.Version == migration.Version && x.Status == MigrationStatus.Errored && x.UpdatedAt == previousRun.UpdatedAt);
                    //if no previous run or deleted successfully, insert new details
                    //if other node already deleted, the deleted count on the current node will be 0
                    if (previousRun == null || result?.DeletedCount == 1)
                    {
                        await migrationDetailsCollection.InsertOneAsync(migration.MigrationDetails);
                        setToProgressSuccessfully = true;
                    }
                }
                else
                {
                    await migrationDetailsCollection.InsertOneAsync(migration.MigrationDetails);
                    setToProgressSuccessfully = true;
                }

                return setToProgressSuccessfully;
            }
            catch (MongoWriteException)
            {
                //if other node already running the migration skip running it
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
    }
}