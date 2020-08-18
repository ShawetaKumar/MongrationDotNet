﻿using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMigration(this IServiceCollection serviceCollection, IMongoDatabase database)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (database == null) throw new ArgumentNullException(nameof(database));

            serviceCollection.AddSingleton<IMigrationRunner>(provider => new MigrationRunner(database));
            return serviceCollection;
        }

        public static IServiceCollection AddMigration(this IServiceCollection serviceCollection, string connectionString, string databaseName)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            serviceCollection.AddSingleton<IMigrationRunner>(provider =>
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                return new MigrationRunner(database);
            });
            return serviceCollection;

        }
    }
}
