# MongrationDotNet

This package is used for migration of MongoDB documents to handle schema changes seamlessly. 
You need not to worry about writing any code of rename/remove fields from your documents. 
Just create a list with your collection and field names. Provide a version and description and you are set. No need to modify your document schema to have version details.

# How to use MongrationDotNet

Install the Nuget Package from the Teamcity Nuget feed

```
PS> install-package MongrationDotNet
```

Add AddMigration in ServiceCollection of your project. You can either pass your IMongoDatabase reference or your MongoDB connection string and database name. It Returns an object of DBMigration which will be used to run the migration


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
These are migrations performed on every document in a given collection. Supply the version number (Semantic Versioning) and an optional description, then simply override the MigrationObjects property to create a dictionary of collection name and fields to rename/remove

```csharp
public class ProductVersion : MigrationCollection
    {
       public override Version Version => new Version(1, 1, 1, 0);
       public override string Description => "Product migration";
        public override Dictionary<string, Dictionary<string, string>> MigrationObjects =>
            new Dictionary<string, Dictionary<string, string>>
            {
                {
                    TestBase.CollectionName, new Dictionary<string, string>
                    {
                        {"name", "productName"},
                        {"store.id", "store.code"},
                        {"createdUtc", ""},
                        {"notAField", "name"}
                    }
                }
            };
    }
```
# How to run migration

Run the Migrate function on the DBMigration object in your starup code

```csharp
public class SetupMongoCollectionOnStartup : IStartupTask
    {
        private readonly DBMigration DbMigration;

        public SetupMongoCollectionOnStartup(DBMigration dbMigration)
        {
            DbMigration = DbMigration;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await DbMigration.Migrate();
        }
    }
```
