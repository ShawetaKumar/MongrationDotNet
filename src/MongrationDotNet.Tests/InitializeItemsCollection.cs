using System;
using System.IO;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace MongrationDotNet.Tests
{
    public class InitializeItemsCollection : SeedingDataMigration
    {
        public override Version Version => new Version(1, 1, 1, 6);
        public override string Description => "Upload documents in collection";

        public override string CollectionName => "items";

        public override void Prepare()
        {
            var document = GetBsonDocument();
            var jsonParsedDocument = GetBsonDocumentFromJsonFile();
            var itemDocumentFromFile = GetItemDocumentFromJsonFile();
            var itemDocument = GetItem();
            
            Seed(document);
            Seed(jsonParsedDocument);
            Seed(itemDocumentFromFile);
            Seed(itemDocument);
        }

        private BsonDocument GetBsonDocument()
        {
            return new BsonDocument {
                { "type", "product" },
                { "ProductName", "Books" },
                {
                    "targetGroup",
                    new BsonArray {
                        new BsonDocument { { "buyer", "Youngsters" }, { "sellingPitch", "Fiction" } },
                        new BsonDocument { { "buyer", "Working Professional" }, { "sellingPitch", "Work Life Balance" } }
                    }
                }, { "class_id", 480 }
            };
        }

        private BsonDocument GetBsonDocumentFromJsonFile()
        {
            var json = File.ReadAllText(TestBase.FilePath);
            return BsonDocument.Parse(json);
        }

        private BsonDocument GetItemDocumentFromJsonFile()
        {
            var item = JsonConvert.DeserializeObject<Item>(File.ReadAllText(TestBase.FilePath));
            item.ProductName = "Camera";
            return item.ToBsonDocument();
        }

        private BsonDocument GetItem()
        {
            return new Item
            {
                Type = "product",
                ProductName = "Stationary",
                TargetGroup = new[]
                {
                    new TargetGroup
                    {
                        Buyer = "School Kids",
                        SellingPitch = "Safe Colorful Material"
                    },
                    new TargetGroup
                    {
                        Buyer = "Working Professional",
                        SellingPitch = "Durable Material"
                    }
                }
            }.ToBsonDocument();
        }
    }
}