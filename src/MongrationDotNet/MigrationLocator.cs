using System;
using System.Collections.Generic;
using System.Linq;

namespace MongrationDotNet
{
    public static class MigrationLocator
    {
        public static IEnumerable<Migration> GetAllMigrations()
        {
            var migrationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Migration)) && !type.IsAbstract);


            var migrations = new List<Migration>();

            foreach (var type in migrationTypes)
            {
                if (type.BaseType == typeof(CollectionMigration))
                {
                    migrations.Add(Activator.CreateInstance(type) as CollectionMigration);
                }
                if (type.BaseType == typeof(DatabaseMigration))
                {
                    migrations.Add(Activator.CreateInstance(type) as DatabaseMigration);
                }
            }

            return migrations;
        }
    }
}