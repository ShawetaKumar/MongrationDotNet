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
                { "Type", "product" },
                { "ProductName", "Books" },
                {
                    "Store",
                    new BsonDocument { { "Id", "1" }, { "Country", "UK" } }
                },
                {
                    "Sales",
                    new BsonArray {20, 30, 40}
                },
                {
                    "TargetGroup",
                    new BsonArray {
                        new BsonDocument { { "Buyer", "Youngsters" }, { "SellingPitch", "Fiction" } },
                        new BsonDocument { { "Buyer", "Working Professional" }, { "SellingPitch", "Work Life Balance" } }
                    }
                },
                { "Rating", "5*" }
            };
        }
        private BsonDocument GetItem()
        {
            return new Item
            {
                Type = "product",
                ProductName = "Stationary",
                Sales = new [] {100, 127, 167},
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