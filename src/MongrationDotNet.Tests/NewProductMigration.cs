using System;

namespace MongrationDotNet.Tests
{
    public class NewProductMigration : CollectionMigration
    {
        public override Version Version => new Version(1, 1, 1, 1);
        public override string Description => "New Product migration";
        public override bool MigrateArrayValues { get; } = false;

        public NewProductMigration()
        {
            const string collectionName = "newProduct";
            //Array Fields
            MigrationFields.AddPropertyForRenameToMigration(collectionName, "targetGroup.$[].type", "targetGroup.$[].buyer");
            MigrationFields.AddPropertyForRenameToMigration(collectionName, "store.sales.$[].territory", "store.sales.$[].region");
            MigrationFields.AddPropertyForRenameToMigration(collectionName, "bestseller.models.$[].variants.$[].inStock", "bestseller.models.$[].variants.$[].isInStock");
        }
    }
}