using System;
using System.Collections.Generic;
using System.Linq;

namespace MongrationDotNet
{
    public static class MigrationLocator
    {
        public static IEnumerable<DocumentMigration> GetAllDocumentMigrations()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(DocumentMigration)))
                .Select(Activator.CreateInstance)
                .OfType<DocumentMigration>();
        }
    }
}