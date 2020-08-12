using System.Collections.Generic;

namespace MongrationDotNet
{
    public abstract class CollectionMigration : Migration
    {
        public override string Type { get; } = Constants.CollectionMigrationType;
        public virtual Dictionary<string, Dictionary<string, string>> MigrationObjects { get; }
    }
}