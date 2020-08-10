using System;
using System.Collections.Generic;
using System.Linq;

namespace MongrationDotNet
{
    public static class MigrationLocator
    {
        public static IEnumerable<MigrationCollection> GetAllMigrations()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(MigrationCollection)))
                .Select(Activator.CreateInstance)
                .OfType<MigrationCollection>();
        }
    }
}