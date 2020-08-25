using System;

namespace MongrationDotNet.Tests
{
    public class NewProductMigration : CollectionMigration
    {
        public override Version Version => new Version(1, 1, 1, 1);
        public override string Description => "New Product migration";
        public override bool MigrateArrayValues { get; } = false;
        public override string CollectionName => "newProduct";

        public override void Prepare()
        {
            //Array Fields
            MigrationFields.AddRenameProperty("targetGroup.$[].type", "targetGroup.$[].buyer");
            MigrationFields.AddRenameProperty("store.sales.$[].territory", "store.sales.$[].region");
            MigrationFields.AddRenameProperty("bestseller.models.$[].variants.$[].inStock",
                "bestseller.models.$[].variants.$[].isInStock");
        }
    }
}