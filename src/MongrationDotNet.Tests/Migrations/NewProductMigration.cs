using System;

namespace MongrationDotNet.Tests.Migrations
{
    public class NewProductMigration : CollectionMigration
    {
        public override Version Version => new Version(1, 1, 1, 5);
        public override string Description => "New Product migration";
        public override bool MigrateArrayValues { get; } = false;
        public override string CollectionName => "newProduct";

        public override void Prepare()
        {
            //Array Fields
            AddPropertyRename("targetGroup.$[].type", "targetGroup.$[].buyer");
            AddPropertyRename("store.sales.$[].territory", "store.sales.$[].region");
            AddPropertyRename("bestseller.models.$[].variants.$[].inStock",
                "bestseller.models.$[].variants.$[].isInStock");
        }
    }
}