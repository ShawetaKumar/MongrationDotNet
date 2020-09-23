using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongrationDotNet;

namespace SimpleApi
{
    public class ClientSideUpdateDocument : ClientSideDocumentMigration
    {
        public override Version Version => new Version(1, 1, 1, 9);
        public override string Description => "Upload documents in collection by restructuring document in client code";
        public override string CollectionName => "items";
        public override int BatchSize { get; } = 1;
        public override bool RerunMigration => true;

        public override void Prepare()
        {
            //No preparation required
        }

        public override BsonDocument MigrateDocument(BsonDocument document)
        {
            document.AsBsonDocument.TryGetElement("TargetGroup", out var element);
            var bsonValue = element.Value;
            var updatedValues = new List<string>();
            if (bsonValue.IsBsonArray)
            {
                var array = bsonValue.AsBsonArray;
                foreach (var arrayElement in array)
                {
                    arrayElement.AsBsonDocument.TryGetElement("Buyer", out var buyer);
                    arrayElement.AsBsonDocument.TryGetElement("SellingPitch", out var sellingPitch);
                    var newValue = $"{buyer.Value} - {sellingPitch.Value}";
                    updatedValues.Add(newValue);
                }
            }

            document.Set("NewTargetGroup", ToBsonDocumentArray(updatedValues));
            return document;
        }

        public static BsonArray ToBsonDocumentArray(List<string> itemList)
        {
            var array = new BsonArray();
            foreach (var item in itemList)
            {
                array.Add(item);
            }

            return array;
        }
    }
}