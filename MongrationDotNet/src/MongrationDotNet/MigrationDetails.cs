using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongrationDotNet
{
   [BsonIgnoreExtraElements]
    public class MigrationDetails
    {
        public Version Version { get; set; }
        public DateTime AppliedOn { get; set; }
        public string Description { get; set; }

        public void SetMigrationDetails(Version version, string description)
        {
            Version = version;
            Description = description;
            AppliedOn = DateTime.UtcNow;
        }
    }
}