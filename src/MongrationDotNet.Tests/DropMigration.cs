using System;
using System.Collections.Generic;

namespace MongrationDotNet.Tests
{
    public class DropMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 1);
        public override string Description => "Database setup";

        public DropMigration()
        {
            CollectionDropList.Add("myCollection");
            CollectionDropList.Add("notACollection");
            DropIndexList.Add("newCollection1", new List<string> {"newCollection1_name", "newCollection1_status"});
            DropIndexList.Add("newCollection2", new List<string> ());
        }
    }
}