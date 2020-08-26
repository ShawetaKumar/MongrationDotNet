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
            AddCollectionToCreate("product");
            AddCollectionToCreate("sales");
        }
    }
}