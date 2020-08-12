using System;
using System.Collections.Generic;

namespace MongrationDotNet.Tests
{
    public class DatabaseSetUpMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 0);
        public override string Description => "Collection setup";
        public override ICollection<string> CollectionCreationList => new List<string>
        {
            TestBase.CollectionName
        };

        public override Dictionary<string, ICollection<Dictionary<string, SortOrder>>> CreateIndexList =>
            new Dictionary<string, ICollection<Dictionary<string, SortOrder>>>
            {
                {
                    TestBase.CollectionName, new[]
                    {
                        new Dictionary<string, SortOrder> {{"name", SortOrder.Ascending}},
                        new Dictionary<string, SortOrder> {{"status", SortOrder.Descending}},
                        new Dictionary<string, SortOrder> {{"store.id", SortOrder.Ascending}},
                        new Dictionary<string, SortOrder>
                            {{"LastUpdatedUtc", SortOrder.Ascending}, {"_id", SortOrder.Ascending}},
                        new Dictionary<string, SortOrder>
                            {{"_id", SortOrder.Ascending}, {"LastUpdatedUtc", SortOrder.Ascending}},
                    }
                }
            };
        public override Dictionary<string, Dictionary<string, int>> CreateExpiryIndexList =>
            new Dictionary<string, Dictionary<string, int>>
            {
                {
                    TestBase.CollectionName, new Dictionary<string, int>
                    {
                        {"lastUpdatedUtc", 30},
                    }
                }
            };
    }
}