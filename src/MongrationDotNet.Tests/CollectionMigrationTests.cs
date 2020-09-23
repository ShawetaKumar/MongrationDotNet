using System;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Shouldly;

namespace MongrationDotNet.Tests
{
    public class CollectionMigrationTests : TestBase
    {
        private IMongoCollection<BsonDocument> productCollection;
        private Version Version => new Version(1, 1, 1, 4);

        [SetUp]
        public void SetupDatabase()
        {
            Runner.Import(DbName, CollectionName, FilePath, true);
            productCollection = Database.GetCollection<BsonDocument>(CollectionName);
        }

        [TearDown]
        public async Task Reset()
        {
            await ResetMigrationDetails();
        }

       [Test]
        public async Task
            Migration_ShouldExecuteSuccessfullyAndNotThrowError_WhenMigrationObjectListContainsANonExistingField()
        {
            await MigrationRunner.Migrate();
            var result = await MigrationCollection.Find(x=> x.Type == Constants.CollectionMigrationType).SortBy(x=>x.Version).FirstOrDefaultAsync();

            result.ShouldNotBeNull();
            result.Version.ShouldNotBeNull();
            result.Version.ShouldBe(Version);
        }

        [Test]
        public async Task Migration_ShouldRenameField_WhenMigrationObjectListContainsFieldsToRename()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
            var document = result.ToString();
            document.Contains("name").ShouldBeFalse();
            document.Contains("productName").ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldRenameEmbeddedField_WhenMigrationObjectListContainsEmbeddedFieldsToRename()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
            var document = result.ToString();
            document.Contains(
                    "\"productDetails\" : { \"description\" : \"Bluetooth Headphones\", \"brand\" : \"JBL\" }")
                .ShouldBeFalse();
            document.Contains(
                    "\"productDetails\" : { \"description\" : \"Bluetooth Headphones\", \"brandName\" : \"JBL\" }")
                .ShouldBeTrue();
        }

        [Test]
        public async Task Migration_ShouldRemoveField_WhenMigrationObjectListContainsFieldsToRemove()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
            var document = result.ToString();
            document.Contains("createdUtc").ShouldBeFalse();
        }

        [Test]
        public async Task Migration_ShouldRemoveArrayField_WhenMigrationObjectListContainsArrayFieldsToRemove()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

            AssertArraySchema(result, "targetGroup", "age");
            AssertArraySchema(result, "store.sales", "franchise");
            AssertArraySchema(result, "bestseller.models.variants", "type");
        }

        [Test]
        public async Task
            Migration_ShouldRenameArrayFieldAndMigrateItsValue_WhenMigrationObjectListContainsArrayFieldsToRenameAndMigrateArrayValuesIsTrue()
        {
            await MigrationRunner.Migrate();
            var result = await productCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

            AssertArraySchema(result, "targetGroup", "type", "buyer", true);
            AssertArraySchema(result, "store.sales", "territory", "region", true);
            AssertArraySchema(result, "bestseller.models.variants", "inStock", "isInStock", true);
        }

        [Test]
        public async Task
            Migration_ShouldRenameArrayFieldAndSetItsValueToNull_WhenMigrationObjectListContainsArrayFieldsToRenameAndMigrateArrayValuesIsFalse()
        {
            const string collectionName = "newProduct";

            Runner.Import(DbName, collectionName, FilePath, true);
            var collection = Database.GetCollection<BsonDocument>(collectionName);

            await MigrationRunner.Migrate();
            var result = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

            AssertArraySchema(result, "targetGroup", "type", "buyer");
            AssertArraySchema(result, "store.sales", "territory", "region");
            AssertArraySchema(result, "bestseller.models.variants", "inStock", "isInStock");
        }

        private static void AssertArraySchema(BsonDocument document, string arrayName, string oldField,
            string newField = null, bool checkValueForNotNull = false)
        {
            var segments = arrayName.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);

            var currentSegmentIndex = 0;
            BsonValue innerDocument = document.AsBsonDocument;
            foreach (var segment in segments)
            {
                innerDocument = innerDocument.AsBsonDocument[segment];
                var isLastNameSegment = segments.Length == currentSegmentIndex + 1;
                if (!isLastNameSegment && innerDocument.IsBsonArray)
                {
                    var bsonArray = innerDocument.AsBsonArray;
                    foreach (var arrayElement in bsonArray)
                    {
                        innerDocument = arrayElement;
                    }
                }

                currentSegmentIndex += 1;
            }

            var array = innerDocument.AsBsonArray;
            foreach (var arrayElement in array)
            {
                var fieldName = arrayElement.AsBsonValue.ToString();
                fieldName.ShouldNotContain($"\"{oldField}\" :");
                if (newField == null) continue;

                fieldName.ShouldContain($"\"{newField}\" :");

                var fieldValue = arrayElement.AsBsonDocument[newField];
                if (checkValueForNotNull)
                    string.IsNullOrEmpty(fieldValue.ToString()).ShouldBeFalse();
                else
                    fieldValue.ShouldBe(BsonValue.Create(null));
            }
        }
    }
}