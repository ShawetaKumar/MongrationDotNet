using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class Migration : IMigration
    {
        public abstract Version Version { get; }
        public abstract string Type { get; }
        public virtual string Description { get; }
        public virtual bool RerunMigration { get; } = false;
        public virtual TimeSpan ExpiryAfter { get; } = TimeSpan.FromMinutes(30);
        public MigrationDetails MigrationDetails => new MigrationDetails(Version , Type, Description, ExpiryAfter);

        public abstract void Prepare();
        public abstract Task ExecuteAsync(IMongoDatabase database, ILogger logger);
    }
}