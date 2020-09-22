using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class MigrationTests : TestBase
    {
        private IMongoCollection<MigrationDetails> migrationCollection;

        [SetUp]
        public void SetupTests()
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
        [Order(1)]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();
            var result = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Status.ShouldBe(MigrationStatus.Completed);
        }

        [Test]
        [Order(2)]
        public async Task Migration_ShouldApplyAllMigrations_WhenMultipleMigrationExists()
        {
            await MigrationRunner.Migrate();

            var migrations = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty)
                .ToListAsync();

            migrations.ShouldNotBeNull();
            migrations.Count.ShouldBe(GetTotalMigrationCount());
        }

        [Test]
        [Order(3)]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyApplied()
        {
            var version = new Version(1, 1, 1, 0);
            var migrationDetails =
                new MigrationDetails(version, Constants.DatabaseMigrationType, "database setup");
            migrationDetails.MarkCompleted();

            await migrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Type == Constants.DatabaseMigrationType && x.Version == version).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result.Single().Status.ShouldBe(MigrationStatus.Completed);
        }

        [Test]
        [Order(4)]
        public async Task Migration_ShouldSkipMigration_WhenMigrationIsSetForRerunButMigrationVersionIsAlreadyCompleted()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");
            migrationDetails.MarkCompleted();

            await migrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });
            var savedMigration = await migrationCollection
                .Find(x => x.Version == version).SingleOrDefaultAsync();

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.UpdatedAt.ShouldBe(savedMigration.UpdatedAt);
        }

        [Test]
        [Order(5)]
        public async Task Migration_ShouldRerunMigration_WhenMigrationIsSetForRerunAndExistingMigrationVersionInDBIsErrored()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");
            migrationDetails.MarkErrored();
            await migrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });

            var savedMigration = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.UpdatedAt.ShouldBeGreaterThan(savedMigration.UpdatedAt);
        }

        [Test]
        [Order(6)]
        public async Task Migration_ShouldBeAppliedOnlyOnce_WhenMultipleTaskRunTheMigration()
        {
            var tasks = new Task[3];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = MigrationRunner.Migrate();
            }

            await Task.WhenAll(tasks);

            var migrations = await migrationCollection.Find(FilterDefinition<MigrationDetails>.Empty)
                .ToListAsync();

            migrations.ShouldNotBeNull();
            migrations.Count.ShouldBe(GetTotalMigrationCount());
        }

        [Test]
        [Order(7)]
        public async Task Migration_ShouldThrowException_WhenMigrationIsAlreadyInProgressAndItDoesNotCompleteBeforeTimeout()
        {
            var version = new Version(1, 1, 1, 0);
            var migrationDetails =
                new MigrationDetails(version, Constants.DatabaseMigrationType, "database setup");
            await migrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });

            Assert.ThrowsAsync<TimeoutException>(async () => await MigrationRunner.Migrate());
        }

        private int GetTotalMigrationCount()
        {
            var migrationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IMigration).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();
            return migrationTypes.Count;
        }
    }
}