using System;

namespace MongrationDotNet.Tests
{
    public class ProductMigration : CollectionMigration
    {
        public override Version Version => new Version(1, 1, 1, 4);
        public override string Description => "Product migration";
        public override bool MigrateArrayValues { get; } = true;
        public override string CollectionName => TestBase.CollectionName;

        public override void Prepare()
        {
            AddPropertyRename("name", "productName");
            AddPropertyRename("productDetails.brand", "productDetails.brandName");
            AddPropertyRename("notAField", "name");
            AddPropertyRemoval("createdUtc");

            //Array Fields
            AddPropertyRename("targetGroup.$[].type", "targetGroup.$[].buyer");
            AddPropertyRename("store.sales.$[].territory", "store.sales.$[].region");
            AddPropertyRename("bestseller.models.$[].variants.$[].inStock",
                "bestseller.models.$[].variants.$[].isInStock");

            AddPropertyRemoval("targetGroup.$[].age");
            AddPropertyRemoval("store.sales.$[].franchise");
            AddPropertyRemoval("bestseller.models.$[].variants.$[].type");
        }
    }
}