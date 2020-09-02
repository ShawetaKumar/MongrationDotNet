using System;
using MongoDB.Bson;
using MongrationDotNet;

namespace SimpleApi
{
    public class InitializeCollection : SeedingDataMigration
    {
        public override Version Version => new Version(1, 1, 1, 3);
        public override string Description => "Upload documents in collection";

        public override string CollectionName => "items";

        public override void Prepare()
        {
            var document = GetBsonDocument();
            var productDocument = GetItem();

            Seed(document);
            Seed(productDocument);
        }

        private BsonDocument GetBsonDocument()
        {
            return new BsonDocument {
                { "type", "product" },
                { "productName", "Books" },
                {
                    "targetGroup",
                    new BsonArray {
                        new BsonDocument { { "buyer", "Youngsters" }, { "sellingPitch", "Fiction" } },
                        new BsonDocument { { "buyer", "Working Professional" }, { "sellingPitch", "Work Life Balance" } }
                    }
                }, { "class_id", 480 }
            };
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