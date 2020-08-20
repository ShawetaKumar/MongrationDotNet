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

Collection Migration:
These are migrations performed on every document in a given collection. Supply the version number (Semantic Versioning) and an optional description, then simply add to the MigrationFields property to create a dictionary of collection name and fields to rename/remove

```csharp
public class ProductMigration : CollectionMigration
{
   public override Version Version => new Version(1, 1, 1, 0);
   public override string Description => "Product migration";
       
   public ProductMigration()
   {
       MigrationObjects.AddPropertyForRenameToMigration("collectionName", "oldFieldName", "newFieldName");
       MigrationObjects.AddPropertyForRemovalToMigration("collectionName", "fieldName");
   }
}
```

Embedded fields are referenced by the dot notation

```json
{
  "type": "Product",
  "name": "Headphones",
  "productDetails": {
    "brand": "JBL",
    "description": "Bluetooth Headphones"
  }
}
```

To rename/remove fields under productDetails:
```csharp
public class ProductMigration : CollectionMigration
{
   public override Version Version => new Version(1, 1, 1, 0);
   public override string Description => "Product migration";
       
   public ProductMigration()
   {
       MigrationObjects.AddPropertyForRenameToMigration("collectionName", "productDetails.description", "productDetails.features");
       MigrationObjects.AddPropertyForRemovalToMigration("collectionName", "productDetails.brand");
   }
}
```
Array fields are referenced by the all positional operator, $[] 

```json
{
  "type": "Product",
  "targetGroup": [
    {
      "age": "15-25",
      "type": "Youngsters"
    },
    {
      "age": "25-45",
      "type": "Female"
    }
  ],
  "store": {
    "id": "s01",
    "sales": [
      {
        "franchise": true,
        "territory": "UK"
      },
      {
        "franchise": false,
        "territory": "US"
      }
    ]
  },
  "bestseller":
  {
    "models": [
      {
        "category": "A",
        "variants": [
          {
            "type": "A",
            "color": "red",
            "inStock": true
          },
          {
            "type": "A",
            "color": "black",
            "inStock": false
          }
        ]
      },
      {
        "category": "B",
        "variants": [
          {
            "type": "B",
            "color": "blue",
            "inStock": true
          },
          {
            "type": "B",
            "color": "white",
            "inStock": false
          }
        ]
      }
    ]
  }
}
```

To rename/remove fields the array fields in the above json:
```csharp
public class ProductMigration : CollectionMigration
{
   public override Version Version => new Version(1, 1, 1, 0);
   public override string Description => "Product migration";
       
   public ProductMigration()
   {
       MigrationFields.AddPropertyForRemovalToMigration("collectionName", "targetGroup.$[].age");
       MigrationFields.AddPropertyForRemovalToMigration("collectionName", "store.sales.$[].franchise");
       MigrationFields.AddPropertyForRemovalToMigration("collectionName", "bestseller.models.$[].variants.$[].type");

       MigrationFields.AddPropertyForRenameToMigration("collectionName", "targetGroup.$[].type", "targetGroup.$[].buyer");
       MigrationFields.AddPropertyForRenameToMigration("collectionName", "store.sales.$[].territory", "store.sales.$[].region");
       MigrationFields.AddPropertyForRenameToMigration("collectionName", "bestseller.models.$[].variants.$[].inStock", "bestseller.models.$[].variants.$[].isInStock");
   }
}
```

By default while renaming the array fields the values in the old field are also retained. However this process is achieved via looping through each document in the collection and hence can be slow depending upon collection size.
If you wish to just to rename the field and do not care about the values also to be migrated then override the MigrateArrayValues property and set it to false. The fields will be renamed and its values will be set to null in all the existing documents 

```csharp
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
```

Database Migration:
These are migrations performed on the database to create/rename/drop collection and create/drop indexes. Supply the version number (Semantic Versioning) and an optional description, then simply add to the appropriate migration property to create a dictionary of collection name and fields to create/rename/drop.
Only add to that dictionary which you need to migrate

```csharp
public class DatabaseSetUpMigration : DatabaseMigration
{
    public override Version Version => new Version(1, 1, 1, 0);
    public override string Description => "Database setup";

    public DatabaseSetUpMigration()
    {
        CollectionCreationList.Add("collectionName");
        CreateIndexList.AddToList("collectionName", "fieldName", SortOrder.Ascending);
        CreateIndexList.AddToList("collectionName", "fieldName", SortOrder.Descending);
        
        //reference embedded fiels by dot notation
        CreateIndexList.AddToList("collectionName", "fieldName.embeddedFieldName", SortOrder.Ascending);
        
        //create a compound index by specifying an array of fields and their corresponding sort order
        CreateIndexList.AddToList(T"collectionName", new []{ "fieldName1" , "fieldName2" }, new[] { SortOrder.Ascending, SortOrder.Ascending
        });
        CreateIndexList.AddToList("collectionName", new []{ "fieldName2", "fieldName1" }, new[] { SortOrder.Ascending, SortOrder.Descending});
        
        //create expiry index by specifying a datetime fieldName and expiry in days
        CreateExpiryIndexList.AddToList("collectionName", "fieldName", 30);
    }
}
```
```csharp
public class DropMigration : DatabaseMigration
{
    public override Version Version => new Version(1, 1, 1, 1);
    public override string Description => "Database setup";

    public DropMigration()
    {
        CollectionDropList.Add(""collectionName"");
        DropIndexList.Add(""collectionName"", new List<string> { "index1_name", "index2_name" });
        //to drop all the indexes from the collection keep the index list empty. Note that it will however not delete the default index on _id
        DropIndexList.Add(""collectionName"", new List<string>());
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
