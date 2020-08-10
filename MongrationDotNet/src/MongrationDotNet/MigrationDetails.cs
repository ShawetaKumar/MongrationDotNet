using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongrationDotNet
{
   [BsonIgnoreExtraElements]
   public class MigrationDetails
   {
       public Version Version { get; set; }
       public string Type { get; set; }
       public string Description { get; set; }
       public DateTime AppliedOn { get; set; }

       public void SetMigrationDetails(Version version, string type, string description)
       {
           Version = version;
           Type = type;
           Description = description;
           AppliedOn = DateTime.UtcNow;
       }
    }
}