using System.Collections.Generic;

namespace MongrationDotNet
{
    public abstract class DatabaseMigration : Migration
    {
        public override string Type { get; } = Constants.DatabaseMigrationType;
        public virtual ICollection<string> CollectionCreationList { get; } = new List<string>();
        public virtual ICollection<string> CollectionDropList { get; } = new List<string>();
        public virtual Dictionary<string, string> CollectionRenameList { get; } = new Dictionary<string, string>();
        public virtual Dictionary<string, ICollection<Dictionary<string, SortOrder>>> CreateIndexList { get; } = new Dictionary<string, ICollection<Dictionary<string, SortOrder>>>();
        public virtual Dictionary<string, Dictionary<string, int>> CreateExpiryIndexList { get; } = new Dictionary<string, Dictionary<string, int>>();
        public virtual Dictionary<string, ICollection<string>> DropIndexList { get; } = new Dictionary<string, ICollection<string>>();
    }
}