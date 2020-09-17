using System;
using System.IO;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace MongrationDotNet.Tests.Migrations
{
    public class InitializeCollection_BsonDocument : SeedingDataMigration<BsonDocument>
    {
        public override Version Version => new Version(1, 1, 1, 6);
        public override string Description => "Upload documents in collection";

        public override string CollectionName => "items";

        public override void Prepare()
        {
            var document = GetBsonDocument();
            var jsonParsedDocument = GetBsonDocumentFromJsonFile();
            
            Seed(document);
            Seed(jsonParsedDocument);
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
    }

    public class InitializeCollection_Item : SeedingDataMigration<Item>
    {
        public override Version Version => new Version(1, 1, 1, 7);
        public override string Description => "Upload documents in collection";

        public override string CollectionName => "items";

        public override void Prepare()
        {
            var itemDocumentFromFile = GetItemDocumentFromJsonFile();
            var itemDocument = GetItem();

            Seed(itemDocumentFromFile);
            Seed(itemDocument);
        }

        private Item GetItemDocumentFromJsonFile()
        {
            var item = JsonConvert.DeserializeObject<Item>(File.ReadAllText(TestBase.FilePath));
            item.ProductName = "Camera";
            return item;
        }

        private Item GetItem()
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
            };
        }
    }
}