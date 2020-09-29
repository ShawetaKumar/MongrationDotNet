using System;
using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongrationDotNet
{
    [BsonIgnoreExtraElements]
    public class MigrationDetails
    {
        public MigrationDetails(Version version, string type, string description, TimeSpan expiryAfter)
        {
            Version = version;
            Type = type;
            Description = description;
            Status = MigrationStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
            ExpireAt = UpdatedAt.Add(expiryAfter);
        }

       [BsonId]
       [BsonRequired]
        public Version Version { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        [BsonRepresentation(BsonType.String)]
        public MigrationStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime ExpireAt { get; set; }

        public void MarkCompleted()
        {
            Status = MigrationStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkErrored(string errorMessage)
        {
            Status = MigrationStatus.Errored;
            UpdatedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
        }

        public bool ShouldRerun(bool rerun)
        {
            return Status == MigrationStatus.InProgress &&
                   rerun && ExpireAt < DateTime.UtcNow || Status == MigrationStatus.Errored &&
                   rerun;
        }
    }

    public enum MigrationStatus
    {
        [Description("InProgress")]InProgress,
        [Description("Completed")] Completed,
        [Description("Errored")] Errored
    }
}