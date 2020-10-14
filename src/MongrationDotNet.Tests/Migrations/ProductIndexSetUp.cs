using System;

namespace MongrationDotNet.Tests.Migrations
{
    public class ProductIndexSetUp : IndexMigration
    {
        public override Version Version => new Version(1, 1, 1, 1);
        public override string Description => "Product index creation";

        public override string CollectionName => TestBase.CollectionName;

        public override void Prepare()
        {
            AddIndex("name", SortOrder.Ascending);
            AddIndex("status", SortOrder.Descending);
            AddIndex("store.id", SortOrder.Ascending);
            AddIndex(new[] { "lastUpdatedUtc", "_id" },
                new[] { SortOrder.Ascending, SortOrder.Ascending });
            AddIndex(new[] { "_id", "lastUpdatedUtc" },
                new[] { SortOrder.Ascending, SortOrder.Ascending });
            AddIndex(new[] { "_id", "status" },
                new[] { SortOrder.Ascending, SortOrder.Ascending }, true);
            AddExpiryIndex("lastUpdatedUtc", 30);
        }
    }
}