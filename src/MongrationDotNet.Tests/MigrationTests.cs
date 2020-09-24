using MongoDB.Driver;
using MongrationDotNet.Tests.Migrations;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongrationDotNet.Tests
{
    public class MigrationTests : TestBase
    {

        [SetUp]
        public async Task SetupTests()
        {
            await ResetMigrationDetails();
            await SetupMigrationDetailsCollection();
        }

        [TearDown]
        public async Task Reset()
        {
            await ResetMigrationDetails();
        }

        [Test]
        [Order(1)]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();
            var result = await MigrationCollection.Find(FilterDefinition<MigrationDetails>.Empty).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Status.ShouldBe(MigrationStatus.Completed);
        }

        [Test]
        [Order(2)]
        public async Task Migration_ShouldApplyAllMigrations_WhenMultipleMigrationExists()
        {
            await MigrationRunner.Migrate();

            var migrations = await MigrationCollection.Find(FilterDefinition<MigrationDetails>.Empty)
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

            await MigrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });

            var migrationsApplied = await MigrationRunner.Migrate();

            migrationsApplied.Count(x=>x.Version == version).ShouldBe(0);
        }

        [Test]
        [Order(4)]
        public async Task Migration_ShouldSkipMigration_WhenMigrationIsSetForRerunButMigrationVersionIsAlreadyCompleted()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");
            migrationDetails.MarkCompleted();

            await MigrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });
            
            var migrationsApplied = await MigrationRunner.Migrate();

            migrationsApplied.Count(x => x.Version == version).ShouldBe(0);
        }

        [Test]
        [Order(5)]
        public async Task Migration_ShouldRerunMigration_WhenMigrationIsSetForRerunAndExistingMigrationVersionInDBIsErrored()
        {
            var version = new Version(1, 1, 1, 7);
            var migrationDetails =
                new MigrationDetails(version, Constants.ClientSideDocumentMigrationType, "database setup");
            migrationDetails.MarkErrored("Test ErrorMessage");
            await MigrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });

            var savedMigration = await MigrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            var migrationsApplied = await MigrationRunner.Migrate();
            var result = await MigrationCollection
                .Find(x => x.Version == version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.UpdatedAt.ShouldBeGreaterThan(savedMigration.UpdatedAt);
            result.Status.ShouldBe(MigrationStatus.Completed);
            result.ErrorMessage.ShouldBeNull();
            migrationsApplied.Count(x => x.Version == version).ShouldBe(1);
        }

        [Test]
        [Order(6)]
        public async Task Migration_ShouldBeAppliedOnlyOnce_WhenMultipleTaskRunTheMigration()
        {
            var tasks = new Task<List<MigrationDetails>>[3];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = MigrationRunner.Migrate();
            }

            await Task.WhenAll(tasks);

            var migrations = await MigrationCollection.Find(FilterDefinition<MigrationDetails>.Empty)
                .ToListAsync();

            migrations.ShouldNotBeNull();
            migrations.Count.ShouldBe(GetTotalMigrationCount());

            var migrationsApplied = tasks.SelectMany(x => x.Result).ToArray();
            var distinctVersions = migrationsApplied.Select(x => x.Version).Distinct();
            distinctVersions.Count().ShouldBe(migrationsApplied.Count());
        }

        [Test]
        [Order(7)]
        public async Task Migration_ShouldThrowException_WhenMigrationIsAlreadyInProgressAndItDoesNotCompleteBeforeTimeout()
        {
            var version = new Version(1, 1, 1, 0);
            var migrationDetails =
                new MigrationDetails(version, Constants.DatabaseMigrationType, "database setup");
            await MigrationCollection.ReplaceOneAsync(x => x.Version == version, migrationDetails,
                new ReplaceOptions { IsUpsert = true });

            Assert.ThrowsAsync<TimeoutException>(async () => await MigrationRunner.Migrate());
        }

        [Test]
        [Order(8)]
        public async Task Migration_ShouldSaveMigrationDetailsWithError_WhenMigrationGeneratedError()
        {
            var version = MigrationForErrorTest.version;
            await MigrationRunner.Migrate();
            var result = await MigrationCollection.Find(x=> x.Version == version).SingleOrDefaultAsync();

            result.ShouldNotBeNull();
            result.ErrorMessage.ShouldNotBeNull();
            result.Status.ShouldBe(MigrationStatus.Errored);
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