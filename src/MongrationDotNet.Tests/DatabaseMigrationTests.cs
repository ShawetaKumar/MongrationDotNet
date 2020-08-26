using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class DatabaseMigrationTests : TestBase
    {
        private IMongoCollection<MigrationDetails> migrationCollection;

        private Version Version => new Version(1, 1, 1, 0);

        [SetUp]
        public async Task SetupDatabase()
        {
            await Database.CreateCollectionAsync(CollectionName);
            migrationCollection = Database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
        }

        [TearDown]
        public async Task ResetMigrationDetails()
        {
            await Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
        }

        [Test]
        public async Task Migration_ShouldSaveMigrationDetails_WhenMigrationIsApplied()
        {
            await MigrationRunner.Migrate();

            var result = await migrationCollection.Find(x => x.Type == Constants.DatabaseMigrationType)
                .FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(Version);
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyInProgress()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.DatabaseMigrationType, "database migration");
            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();

            var result = await migrationCollection
                .Find(x => x.Version == Version && x.Type == Constants.DatabaseMigrationType).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldSkipMigration_WhenMigrationVersionIsAlreadyApplied()
        {
            var migrationDetails = new MigrationDetails(Version, Constants.DatabaseMigrationType, "database migration");
            migrationDetails.MarkCompleted();
            await migrationCollection.InsertOneAsync(migrationDetails);

            await MigrationRunner.Migrate();

            var result = await migrationCollection
                .Find(x => x.Version == Version && x.Type == Constants.DatabaseMigrationType).ToListAsync();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
        }

        [Test]
        public async Task Migration_ShouldCreateCollection_WhenCreationListContainsCollectionNames()
        {
            await MigrationRunner.Migrate();

            var filter = new BsonDocument("name", CollectionName);
            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});

            var exits = await collections.AnyAsync();
            exits.ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldDropCollection_WhenDropListContainsCollectionNames()
        {
            const string collectionName = "myCollection";
            await Database.CreateCollectionAsync(collectionName);
            await MigrationRunner.Migrate();
            var filter = new BsonDocument("name", collectionName);
            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});

            var exists = await collections.AnyAsync();
            exists.ShouldBeFalse();
        }

        [Test]
        public async Task Migration_ShouldRenameCollection_WhenRenameListContainsCollectionNames()
        {
            const string oldCollection = "oldCollection";
            const string newCollection = "newCollection";
            await Database.CreateCollectionAsync(oldCollection);

            await MigrationRunner.Migrate();

            var filter = new BsonDocument("name", oldCollection);
            var collections = await Database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            var exists = await collections.AnyAsync();

            exists.ShouldBeFalse();

            filter = new BsonDocument("name", newCollection);
            collections = await Database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            exists = await collections.AnyAsync();

            exists.ShouldBeTrue();
        }

        [Test]
        public async Task
            Migration_ShouldExecuteSuccessfullyAndNotThrowError_WhenDropListContainsANonExistingCollection()
        {
            var version = new Version(1, 1, 1, 1);
            await MigrationRunner.Migrate();

            var result = await migrationCollection
                .Find(x => x.Version == version && x.Type == Constants.DatabaseMigrationType).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(version);
        }
    }
}