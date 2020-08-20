using System;

namespace MongrationDotNet.Tests
{
    public class ProductMigration : CollectionMigration
    {
       public override Version Version => new Version(1, 1, 1, 0);
       public override string Description => "Product migration";
       public override bool MigrateArrayValues { get; } = true;

        public ProductMigration()
       {
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "name", "productName");
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "productDetails.brand", "productDetails.brandName");
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "notAField", "name");
           MigrationFields.AddPropertyForRemovalToMigration(TestBase.CollectionName, "createdUtc");
           
           //Array Fields
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "targetGroup.$[].type", "targetGroup.$[].buyer");
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "store.sales.$[].territory", "store.sales.$[].region");
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "bestseller.models.$[].variants.$[].inStock", "bestseller.models.$[].variants.$[].isInStock");

           MigrationFields.AddPropertyForRemovalToMigration(TestBase.CollectionName, "targetGroup.$[].age");
           MigrationFields.AddPropertyForRemovalToMigration(TestBase.CollectionName, "store.sales.$[].franchise");
           MigrationFields.AddPropertyForRemovalToMigration(TestBase.CollectionName, "bestseller.models.$[].variants.$[].type");
           
        }
    }
}