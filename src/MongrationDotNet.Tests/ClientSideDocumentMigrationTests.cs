using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class ClientSideDocumentMigrationTests : TestBase
    {
        private readonly string collectionName = "item";
        private IMongoCollection<BsonDocument> itemCollection;
        private IMongoCollection<MigrationDetails> migrationCollection;

        [SetUp]
        public void SetupDatabase()
        {
            runner.Import(DbName, collectionName, FilePath, true);
            migrationCollection = Database.GetCollection<MigrationDetails>(Constants.MigrationDetailsCollection);
            itemCollection = Database.GetCollection<BsonDocument>(collectionName);
        }

        [TearDown]
        public async Task ResetMigrationDetails()
        {
            await Database.ListCollectionNames().ForEachAsync(async x => await Database.DropCollectionAsync(x));
        }

        [Test]
        public async Task Migration_ShouldMigrateAllDocuments_WhenDocumentsAreRestructuredAtClientSide()
        {
            await MigrationRunner.Migrate();
            var documents = await itemCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            foreach (var document in documents)
            {
                document.AsBsonDocument.TryGetElement("newTargetGroup", out var element);
                element.ShouldNotBeNull();
                element.Value.AsBsonArray.ShouldNotBeNull();
                document.AsBsonDocument.TryGetElement("targetGroup", out element);
                element.Value.ShouldBeNull();
            }
        }
    }
}