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

        [SetUp]
        public void SetupDatabase()
        {
            Runner.Import(DbName, collectionName, FilePath, true);
            itemCollection = Database.GetCollection<BsonDocument>(collectionName);
        }

        [TearDown]
        public async Task Reset()
        {
            await ResetMigrationDetails();
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