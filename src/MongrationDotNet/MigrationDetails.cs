﻿using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongrationDotNet
{
    [BsonIgnoreExtraElements]
    public class MigrationDetails
    {
        public MigrationDetails(Version version, string type, string description)
        {
            Version = version;
            Type = type;
            Description = description;
            Status = MigrationStatus.InProgress;
        }

       [BsonId]
       [BsonRequired]
        public Version Version { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public MigrationStatus Status { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void MarkCompleted()
        {
            Status = MigrationStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkErrored()
        {
            Status = MigrationStatus.Errored;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum MigrationStatus
    {
        InProgress,
        Completed,
        Errored
    }
}