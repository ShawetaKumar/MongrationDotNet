using System;

namespace MongrationDotNet.Tests.Migrations
{
    public class MigrationForErrorTest : ServerSideDocumentMigration
    {
        //This should always be the last migration to run else other migrations will be skipped
        public static Version version = new Version(10, 0, 0, 0);
        public override Version Version => version;
        public override string Description => "Index migration with error";
        public override string CollectionName => "items";

        public override void Prepare()
        {
            AddMigrationField("ProductDetails", "{ $errorExpression: \"$Type\", \" - \", \"$ProductName\" ] }");
        }
    }
}