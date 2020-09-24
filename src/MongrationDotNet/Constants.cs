namespace MongrationDotNet
{
    /// <summary>
    /// Constants for different migration types
    /// </summary>
    public static class Constants
    {
        public const string MigrationDetailsCollection = "migrationDetails";
        public const string CollectionMigrationType = "CollectionMigration";
        public const string DatabaseMigrationType = "DatabaseMigration";
        public const string IndexMigrationType = "IndexMigration";
        public const string SeedingDataMigrationType = "SeedingDataMigration";
        public const string ServerSideDocumentMigrationType = "ServerSideDocumentMigration";
        public const string ClientSideDocumentMigrationType = "ClientSideDocumentMigration";
    }
}