using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class MigrationTests : TestBase
    {
        private IMongoCollection<MigrationDetails> migrationCollection;
        private Version Version => new Version(1, 1, 1, 0);

        [SetUp]
        public void SetupDatabase()
        {
            Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
            migrationCollection = Database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
        }

        [TearDown]
        public void ResetMigrationDetails()
        {
            Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
        }

        [Test]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();
            var result = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Status.ShouldBe(MigrationStatus.Completed);
        }

        [Ignore("These tests are nor running in combination with other tests. Enable this after fixing concurrency")]
        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyApplied()
        {
            var migrationDetails =
                new MigrationDetails(Version, Constants.DatabaseMigrationType, "database setup");
            migrationDetails.MarkCompleted();

            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Type == Constants.DatabaseMigrationType && x.Version == Version).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldApplyAllMigrations_WhenMultipleMigrationExists()
        {
            await MigrationRunner.Migrate();

            var migrationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IMigration).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();
            
            var migrations = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty)
                .ToListAsync();
            
            migrations.ShouldNotBeNull();
            migrations.Count.ShouldBe(migrationTypes.Count);
        }

        [Ignore("These tests are nor running in combination with other tests. Enable this after fixing concurrency")]
        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationIsSetForRerunButMigrationVersionIsAlreadyCompleted()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");
            migrationDetails.MarkCompleted();

            await migrationCollection.InsertOneAsync(migrationDetails);
            var savedMigration = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.UpdatedAt.ShouldBe(savedMigration.UpdatedAt);
        }

        [Ignore("These tests are nor running in combination with other tests. Enable this after fixing concurrency")]
        [Test]
        public async Task Migration_ShouldRerunMigration_WhenMigrationIsSetForRerunAndExistingMigrationVersionInDBIsInProgress()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");

            await migrationCollection.InsertOneAsync(migrationDetails);
            var savedMigration = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.UpdatedAt.ShouldBeGreaterThan(savedMigration.UpdatedAt);
        }

        [Ignore("These tests are nor running in combination with other tests. Enable this after fixing concurrency")]
        [Test]
        public async Task Migration_ShouldRerunMigration_WhenMigrationIsSetForRerunAndExistingMigrationVersionInDBIsErrored()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");
            migrationDetails.MarkErrored();
            await migrationCollection.InsertOneAsync(migrationDetails);
            var savedMigration = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.UpdatedAt.ShouldBeGreaterThan(savedMigration.UpdatedAt);
        }
    }
}