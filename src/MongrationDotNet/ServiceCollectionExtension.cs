using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMigration(this IServiceCollection services, IMongoDatabase database)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (database == null) throw new ArgumentNullException(nameof(database));

            services.AddSingleton(provider => database);
            services.AddDependencies();
            return services;
        }

        public static IServiceCollection AddMigration(this IServiceCollection services, string connectionString,
            string databaseName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            services.AddSingleton(provider =>
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                return database;
            });
            services.AddDependencies();
            return services;
        }

        private static void AddDependencies(this IServiceCollection services)
        {
            services.AddTransient<IMigrationRunner, MigrationRunner>();
        }

        public static IServiceCollection WithAllAvailableMigrations(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var migrationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Migration)) && !type.IsAbstract)
                .ToList();

            migrationTypes.ForEach(x => services.AddTransient(typeof(IMigration), x));

            return services;
        }

        public static IServiceCollection With<T>(this IServiceCollection services) where T : class, IMigration
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddTransient<IMigration, T>();

            return services;
        }
    }
}