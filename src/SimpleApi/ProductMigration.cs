using System;
using MongrationDotNet;

namespace SimpleApi
{
    public class ProductMigration : CollectionMigration
    {
        public override Version Version => new Version(1, 1, 1, 0);
        public override string Description => "Product migration";
        public override bool MigrateArrayValues { get; } = true;
        public override string CollectionName => "product";

        public override void Prepare()
        {
            MigrationFields.AddRenameProperty("name", "productName");
            MigrationFields.AddRenameProperty("productDetails.brand", "productDetails.brandName");
            MigrationFields.AddRemoveProperty("createdUtc");
        }
    }
}