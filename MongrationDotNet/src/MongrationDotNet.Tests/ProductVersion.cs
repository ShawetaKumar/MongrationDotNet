using System;
using System.Collections.Generic;

namespace MongrationDotNet.Tests
{
    public class ProductVersion : MigrationCollection
    {
        public override Version Version => new Version(1, 1, 1, 0);
       public override string Description => "Product migration";
        public override Dictionary<string, Dictionary<string, string>> MigrationObjects =>
            new Dictionary<string, Dictionary<string, string>>
            {
                {
                    TestBase.CollectionName, new Dictionary<string, string>
                    {
                        {"name", "productName"},
                        {"store.id", "store.code"},
                        {"createdUtc", ""},
                        {"notAField", "name"}
                    }
                }
            };
    }
}