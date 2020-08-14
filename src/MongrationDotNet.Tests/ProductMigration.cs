using System;

namespace MongrationDotNet.Tests
{
    public class ProductMigration : CollectionMigration
    {
       public override Version Version => new Version(1, 1, 1, 0);
       public override string Description => "Product migration";
       
       public ProductMigration()
       {
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "name", "productName");
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "store.id", "store.code");
           MigrationFields.AddPropertyForRenameToMigration(TestBase.CollectionName, "notAField", "name");
           MigrationFields.AddPropertyForRemovalToMigration(TestBase.CollectionName, "createdUtc");
       }
    }
}