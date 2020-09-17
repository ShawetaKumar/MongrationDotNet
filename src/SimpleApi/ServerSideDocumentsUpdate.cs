using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongrationDotNet;

namespace SimpleApi
{
    public class ServerSideDocumentsUpdateRevision5 : ServerSideDocumentMigration
    {
        public override Version Version => new Version(1, 1, 1, 5);
        public override string Description => "documents update to new schema";
        public override string CollectionName => "items";

        public override void Prepare()
        {
            AddMigrationField("ProductDetails", "{ $concat: [ \"$Type\", \" - \", \"$ProductName\" ] }");
            AddMigrationField("ProductType", "\"$Type\"");
        }
    }

    public class ServerSideDocumentsUpdateRevision6 : ServerSideDocumentMigration
    {
        public override Version Version => new Version(1, 1, 1, 6);
        public override string Description => "documents update to new schema";
        public override string CollectionName => "items";
        public override FilterDefinition<BsonDocument> Filters => BuildFilters();

        public override void Prepare()
        {
            AddMigrationField("Store.Region", "{ $concat: [ \"North \", \"$Store.Country\" ] }");
            AddMigrationField("Sales", "{ $concatArrays: [ \"$Sales\", [ 55 ] ] }");
            AddMigrationField("Ratings", "[ \"A\", \"B\" , \"$Rating\" ]");
        }

        private static FilterDefinition<BsonDocument> BuildFilters()
        {
            var filterBuilder = new FilterDefinitionBuilder<BsonDocument>();
            var idFilter = filterBuilder.Eq("ProductName", "Books");
            var filter = filterBuilder.And(idFilter);
            return filter;
        }
    }

    public class ServerSideDocumentsUpdateRevision7 : ServerSideDocumentMigration
    {
        public override Version Version => new Version(1, 1, 1, 7);
        public override string Description => "documents update to new schema";
        public override string CollectionName => "items";
        public override PipelineDefinition<BsonDocument, BsonDocument> PipelineDefinition { get; set; }

        public override void Prepare()
        {
            PipelineDefinition = BuildPipelineDefinition();
        }

        private static PipelineDefinition<BsonDocument, BsonDocument> BuildPipelineDefinition()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                               .AppendStage("{ $set: { \"TotalSales\": { $sum: \"$Sales\" }, \"Status\": \"Approved\", LastModified: \"$$NOW\" } }",
                                   BsonDocumentSerializer.Instance)
                               .AppendStage("{ $unset: \"Rating\" }", BsonDocumentSerializer.Instance);
            return pipeline;
        }
    }

    public class ServerSideDocumentsUpdateRevision8 : ServerSideDocumentMigration
    {
        public override Version Version => new Version(1, 1, 1, 8);
        public override string Description => "documents update to new schema";
        public override string CollectionName => "items";
        public override PipelineDefinition<BsonDocument, BsonDocument> PipelineDefinition { get; set; }

        public override void Prepare()
        {
            PipelineDefinition = BuildPipelineDefinition();
        }

        private static PipelineDefinition<BsonDocument, BsonDocument> BuildPipelineDefinition()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .AppendStage("{ $addFields: { \"TargetGroup\": { \"$map\": { \"input\": \"$TargetGroup\", \"as\": \"row\", \"in\": { \"Buyer\": \"$$row.Buyer\", \"SellingPitch\": \"$$row.SellingPitch\"," + 
                             " \"Genre\": \"$$row.SellingPitch\"  } } }}}",
                    BsonDocumentSerializer.Instance);
            return pipeline;
        }
    }
}