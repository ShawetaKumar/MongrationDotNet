using System;
using System.Collections.Generic;

namespace MongrationDotNet
{
    public abstract class DocumentMigration
    {
        public abstract Version Version { get; }
        public virtual string Type { get; } = Constants.DocumentMigrationType;
        public virtual string Description { get; }
        public virtual Dictionary<string, Dictionary<string, string>> MigrationObjects { get; }
        
    }
}