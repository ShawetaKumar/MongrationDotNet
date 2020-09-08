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

        [Ignore("Enable this after fixing concurrency")]
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
    }
}