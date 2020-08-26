using System;
using System.Collections.Generic;

namespace MongrationDotNet.Tests
{
    public class DropMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 1);
        public override string Description => "Database setup";

        public override void Prepare()
        {
            AddCollectionToDrop("myCollection");
            AddCollectionToDrop("notACollection");
        }
    }
}