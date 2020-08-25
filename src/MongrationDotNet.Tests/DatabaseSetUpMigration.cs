using System;

namespace MongrationDotNet.Tests
{
    public class DatabaseSetUpMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 0);
        public override string Description => "Database setup";

        public override void Prepare()
        {
            CollectionCreationList.Add(TestBase.CollectionName);
            CreateIndexList.AddToList(TestBase.CollectionName, "name", SortOrder.Ascending);
            CreateIndexList.AddToList(TestBase.CollectionName, "status", SortOrder.Descending);
            CreateIndexList.AddToList(TestBase.CollectionName, "store.id", SortOrder.Ascending);
            CreateIndexList.AddToList(TestBase.CollectionName, new[] {"lastUpdatedUtc", "_id"},
                new[] {SortOrder.Ascending, SortOrder.Ascending});
            CreateIndexList.AddToList(TestBase.CollectionName, new[] {"_id", "lastUpdatedUtc"},
                new[] {SortOrder.Ascending, SortOrder.Ascending});
            CreateExpiryIndexList.AddToList(TestBase.CollectionName, "lastUpdatedUtc", 30);
            CollectionRenameList.AddToList("oldCollection", "newCollection");
        }
    }
}