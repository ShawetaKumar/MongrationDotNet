using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongrationDotNet.Tests.Migrations
{
    public class UpdateDocuments : ClientSideDocumentMigration
    {
        public override Version Version => new Version(1, 1, 1, 8);
        public override string Description => "Upload documents in collection by restructuring document in client code";

        public override string CollectionName => "items";
        public override int BatchSize => 2;

        public override void Prepare()
        {
            //No preparation required
        }

        public override BsonDocument MigrateDocument(BsonDocument document)
        {
            document.AsBsonDocument.TryGetElement("targetGroup", out var element);
            var bsonValue = element.Value;
            var updatedValues = new List<string>();
            if (bsonValue != null && bsonValue.IsBsonArray)
            {
                var array = bsonValue.AsBsonArray;
                foreach (var arrayElement in array)
                {
                    arrayElement.AsBsonDocument.TryGetElement("age", out var age);
                    arrayElement.AsBsonDocument.TryGetElement("type", out var type);
                    var newValue = $"{age.Value} - {type.Value}";
                    updatedValues.Add(newValue);
                }
            }

            document.Set("newTargetGroup", ToBsonDocumentArray(updatedValues));
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