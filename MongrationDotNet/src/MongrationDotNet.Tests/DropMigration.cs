using System;
using System.Collections.Generic;

namespace MongrationDotNet.Tests
{
    public class DropMigration : DatabaseMigration
    {
        public override Version Version => new Version(1, 1, 1, 1);
        public override string Description => "Collection setup";
        public override ICollection<string> CollectionDropList => new List<string>
        {
            "newCollection",
            "notACollection"
        };

        public override Dictionary<string, ICollection<string>> DropIndexList { get; } = new Dictionary<string, ICollection<string>>()
        {
            {
                "newCollection1", new List<string>
                {
                    "newCollection1_name",
                    "newCollection1_status"
                }
            },
            {
                "newCollection2", new List<string>()
            }
        };
    }
}