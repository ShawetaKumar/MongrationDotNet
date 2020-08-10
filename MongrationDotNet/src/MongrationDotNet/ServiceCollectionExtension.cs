using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public static class ServiceCollectionExtension
    {
        public static DBMigration AddMigration(this IServiceCollection serviceCollection, IMongoDatabase database)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (database == null) throw new ArgumentNullException(nameof(database));

            return new DBMigration(database);
        }

        public static DBMigration AddMigration(this IServiceCollection serviceCollection, string connectionString, string databaseName)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            var mongoConnectionUrl = new MongoUrl(connectionString);
            var client = new MongoClient();
            var database = client.GetDatabase(databaseName);
            return new DBMigration(database);
        }
    }
}
