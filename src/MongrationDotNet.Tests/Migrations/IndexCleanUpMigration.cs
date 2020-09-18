using System;

namespace MongrationDotNet.Tests.Migrations
{
    public class IndexCleanUpMigration : IndexMigration
    {
        public override Version Version => new Version(1, 1, 1, 2);
        public override string Description => "newCollection1: Index cleanup";

        public override string CollectionName => "indexCollection";

        public override void Prepare()
        {
            AddToDropIndex("indexCollection_name");
            AddToDropIndex("indexCollection_status"); 
        }
    }
}