# MongrationDotNet

This package is used for the migration of MongoDB documents to handle schema changes seamlessly. 
You need not worry about writing any code of rename/remove fields from your documents. 
Just create a list with your collection and field names. Provide a version and description and you are set. No need to modify your document schema to have version details.

# How to use MongrationDotNet

Install the Nuget Package from the Teamcity Nuget feed

```
PS> install-package MongrationDotNet
```

Add AddMigration in ServiceCollection of your project. You can either pass your IMongoDatabase reference or your MongoDB connection string and database name. It Returns an object of MigrationRunner which will be used to run the migration


```csharp
public void ConfigureServices(IServiceCollection services)
{
    // other dependencies
    
    // Add Migration to your available services
	services.AddMigration(Database);
    //or
    services.AddMigration("connectionString", "databaseName");
}
```

Collection Migration
These are migrations performed on every document in a given collection. Supply the version number (Semantic Versioning) and an optional description, then simply add to the MigrationFields property to create a dictionary of collection name and fields to rename/remove

```csharp
public class ProductMigration : CollectionMigration
{
   public override Version Version => new Version(1, 1, 1, 0);
   public override string Description => "Product migration";
       
   public ProductMigration()
   {
       MigrationObjects.AddPropertyForRenameToMigration(TestBase.CollectionName, "name", "productName");
       MigrationObjects.AddPropertyForRenameToMigration(TestBase.CollectionName, "store.id", "store.code");
       MigrationObjects.AddPropertyForRenameToMigration(TestBase.CollectionName, "notAField", "name");
       MigrationObjects.AddPropertyForRemovalToMigration(TestBase.CollectionName, "createdUtc");
   }
}
```
# How to run the migration

Run the Migrate function on the MigrationRunner object in your startup code

```csharp
public class SetupMongoCollectionOnStartup : IStartupTask
{
    private readonly MigrationRunner migrationRunner;

    public SetupMongoCollectionOnStartup(MigrationRunner migrationRunner)
    {
        this.migrationRunner = migrationRunner;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await migrationRunner.Migrate();
    }
}
```
