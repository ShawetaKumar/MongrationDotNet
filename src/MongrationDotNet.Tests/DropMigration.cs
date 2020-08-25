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
            CollectionDropList.Add("myCollection");
            CollectionDropList.Add("notACollection");
            DropIndexList.AddToList("newCollection1",
                new List<string> {"newCollection1_name", "newCollection1_status"});
            DropIndexList.AddToList("newCollection2", new List<string>());
        }
    }
}