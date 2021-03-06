﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongrationDotNet
{
    /// <summary>
    /// This migration migrates the documents to new schema
    /// For the field value, a static value or an expression (calculation/method like avg/count) can be specified
    /// </summary>
    public abstract class ServerSideDocumentMigration : Migration
    {
        public override string Type { get; } = Constants.ServerSideDocumentMigrationType;

        public abstract string CollectionName { get; }

        public ICollection<(string field, string expression)> MigrationFields { get; } =
            new List<(string, string)>();

        public virtual FilterDefinition<BsonDocument> Filters { get; set; } = FilterDefinition<BsonDocument>.Empty;
        public virtual PipelineDefinition<BsonDocument, BsonDocument> PipelineDefinition { get; set; }


        public override async Task ExecuteAsync(IMongoDatabase database, ILogger logger)
        {
            logger?.LogInformation(LoggingEvents.DocumentMigrationStarted, "Migration started for {collection}",
                CollectionName);

            var collection = database.GetCollection<BsonDocument>(CollectionName);

            var pipelineExpression = new List<string>();
            foreach (var (field, value) in MigrationFields)
            {
                var expression = new StringBuilder()
                    .Append("\"")
                    .Append(field)
                    .Append("\": ")
                    .Append(value);
                pipelineExpression.Add(expression.ToString());
            }

            if (pipelineExpression.Any() || PipelineDefinition != null)
            {
                var pipeline = PipelineDefinition ?? new EmptyPipelineDefinition<BsonDocument>()
                                   .AppendStage("{ $set: { " + string.Join(",", pipelineExpression) + "} }",
                                       BsonDocumentSerializer.Instance);

                await collection.UpdateManyAsync(Filters, pipeline);
            }

            logger?.LogInformation(LoggingEvents.DocumentMigrationCompleted, "Migration completed for {collection}",
                CollectionName);
        }

        /// <summary>
        /// Adds to the Migration field
        /// </summary>
        /// <param name="field">field whose value needs to be set. Can be either new or existing field</param>
        /// <param name="expression">expression to deduce new value</param>
        public void AddMigrationField(string field, string expression)
        {
            if (string.IsNullOrEmpty(field))
                throw new ArgumentException("Value cannot be null or empty.", nameof(field));
            MigrationFields.Add((field, expression));
        }
    }
}