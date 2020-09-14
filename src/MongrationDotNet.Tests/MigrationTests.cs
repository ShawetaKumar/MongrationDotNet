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

        [Ignore("These tests are nor running in combination with other tests. Enable this after fixing concurrency")]
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

            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();
            var result = await migrationCollection
                .Find(x => x.Type == Constants.DatabaseMigrationType && x.Version == version).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result.Single().Status.ShouldBe("Completed");
        }

        [Test]
        [Order(4)]
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