using System;

namespace MongrationDotNet.Tests.Migrations
{
    public class DropMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 3);
        public override string Description => "Database setup";

        public override void Prepare()
        {
            AddCollectionToDrop("myCollection");
            AddCollectionToDrop("notACollection");
        }
    }
}