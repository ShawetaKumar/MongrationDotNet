using System;
using MongrationDotNet;

namespace SimpleApi
{
    public class DatabaseSetUpMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 0);
        public override string Description => "Database setup";

        public override void Prepare()
        {
            var collectionName = "product";
            CollectionCreationList.Add(collectionName);
            CreateIndexList.AddToList(collectionName, "name", SortOrder.Ascending);
            CreateIndexList.AddToList(collectionName, "status", SortOrder.Descending);
            CreateIndexList.AddToList(collectionName, "store.id", SortOrder.Ascending);
            CreateIndexList.AddToList(collectionName, new[] {"lastUpdatedUtc", "_id"},
                new[] {SortOrder.Ascending, SortOrder.Ascending});
            CreateIndexList.AddToList(collectionName, new[] {"_id", "lastUpdatedUtc"},
                new[] {SortOrder.Ascending, SortOrder.Ascending});
            CreateExpiryIndexList.AddToList(collectionName, "lastUpdatedUtc", 30);
        }
    }
}