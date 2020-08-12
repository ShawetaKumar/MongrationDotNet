using System;
using System.Collections.Generic;
using System.Linq;

namespace MongrationDotNet
{
    public static class MigrationLocator
    {
        public static IEnumerable<CollectionMigration> GetAllDocumentMigrations()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(CollectionMigration)))
                .Select(Activator.CreateInstance)
                .OfType<CollectionMigration>();
        }

        public static IEnumerable<DatabaseMigration> GetAllCollectionMigrations()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(DatabaseMigration)))
                .Select(Activator.CreateInstance)
                .OfType<DatabaseMigration>();
        }
    }
}