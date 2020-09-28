using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public interface IMigration
    {
        Version Version { get; }
        string Type { get; }
        string Description { get; }
        bool RerunMigration { get; }
        TimeSpan ExpiryAfter { get; }
        MigrationDetails MigrationDetails { get; }
        void Prepare();
        Task ExecuteAsync(IMongoDatabase database, ILogger logger);
    }
}