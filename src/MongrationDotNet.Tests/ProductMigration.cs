using System;

namespace MongrationDotNet.Tests
{
    public class ProductMigration : CollectionMigration
    {
        public override Version Version => new Version(1, 1, 1, 0);
        public override string Description => "Product migration";
        public override bool MigrateArrayValues { get; } = true;
        public override string CollectionName => TestBase.CollectionName;

        public override void Prepare()
        {
            MigrationFields.AddRenameProperty("name", "productName");
            MigrationFields.AddRenameProperty("productDetails.brand", "productDetails.brandName");
            MigrationFields.AddRenameProperty("notAField", "name");
            MigrationFields.AddRemoveProperty("createdUtc");

            //Array Fields
            MigrationFields.AddRenameProperty("targetGroup.$[].type", "targetGroup.$[].buyer");
            MigrationFields.AddRenameProperty("store.sales.$[].territory", "store.sales.$[].region");
            MigrationFields.AddRenameProperty("bestseller.models.$[].variants.$[].inStock",
                "bestseller.models.$[].variants.$[].isInStock");

            MigrationFields.AddRemoveProperty("targetGroup.$[].age");
            MigrationFields.AddRemoveProperty("store.sales.$[].franchise");
            MigrationFields.AddRemoveProperty("bestseller.models.$[].variants.$[].type");
        }
    }
}