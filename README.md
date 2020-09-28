# MongrationDotNet

This package is used for the migration of MongoDB documents to handle schema changes seamlessly. 
You need not worry about writing any code of rename/remove fields from your documents. 
Just create a list with your collection and field names. Provide a version and description and you are set. No need to modify your document schema to have version details.
Migration version is unique among the different migration types and the versions not applied earlier are only applied. The migration results are saved in the migrationDetails collection. 

# How to use MongrationDotNet

Install the Nuget Package from the Teamcity Nuget feed

```
PS> install-package MongrationDotNet
```

Add AddMigration in ServiceCollection of your project. You can either pass your IMongoDatabase reference or your MongoDB connection string and database name. It Returns an object of MigrationRunner which will be used to run the migration.


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

You can choose to configure with all the available migrations or only with specific migrations.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // other dependencies
    
    // Add Migration to your available services
	services.AddMigration("connectionString", "databaseName")
            .WithAllAvailableMigrations();
    //or
    services.AddMigration("connectionString", "databaseName")
            .With<DatabaseSetUpMigration>()
            .With<ProductMigration>();
}
```


Collection Migration:
These are migrations performed on every document in a given collection. Specify the collection name, version number (Semantic Versioning) and an optional description, then simply add to the MigrationFields property to create a dictionary of collection name and fields to rename/remove

```csharp
public class ProductMigration : CollectionMigration
{
   public override Version Version => new Version(1, 1, 1, 0);
   public override string Description => "Product migration";
   public override string CollectionName => "product";
       
   public override void Prepare()
   {
      AddPropertyRename("name", "productName");
      AddPropertyRemoval("createdUtc");
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
   public override string CollectionName => "product";
       
   public override void Prepare()
   {
      AddPropertyRename("productDetails.description", "productDetails.features");
      AddPropertyRemoval("productDetails.brand");
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
   public override string CollectionName => "product";
       
   public override void Prepare()
   {
      AddPropertyRemoval("targetGroup.$[].age");
      AddPropertyRemoval("store.sales.$[].franchise");
      AddPropertyRemoval("bestseller.models.$[].variants.$[].type");
      
      AddPropertyRename("targetGroup.$[].type", "targetGroup.$[].buyer");
      AddPropertyRename("store.sales.$[].territory", "store.sales.$[].region");
      AddPropertyRename("bestseller.models.$[].variants.$[].inStock", "bestseller.models.$[].variants.$[].isInStock");
   }    
}
```

By default while renaming the array fields the values in the old field are also retained. However this process is achieved via looping through each document in the collection and hence can be slow depending upon collection size.
If you wish to just to rename the field and do not care about the values also to be migrated then override the MigrateArrayValues property and set it to false. The fields will be renamed and its values will be set to null in all the existing documents 

```csharp
public class ProductMigration : CollectionMigration
{
   public override Version Version => new Version(1, 1, 1, 0);
   public override string Description => "Product migration";
   public override string CollectionName => "product";
   public override bool MigrateArrayValues { get; } = false;
       
   public override void Prepare()
   {
      AddPropertyRename("targetGroup.$[].type", "targetGroup.$[].buyer");
      AddPropertyRename("store.sales.$[].territory", "store.sales.$[].region");
      AddPropertyRename("bestseller.models.$[].variants.$[].inStock", "bestseller.models.$[].variants.$[].isInStock");
   }    
}
```

Database Migration:
These are migrations performed on the database to create/rename/drop collection. Specify the version number (Semantic Versioning) and an optional description, then simply add to the appropriate migration property to create/rename/drop.
Only add to that list which you need to migrate

```csharp
public class DatabaseSetUpMigration : DatabaseMigration
{
    public override Version Version => new Version(1, 1, 1, 0);
    public override string Description => "Database setup";

    public override void Prepare()
    {
        AddCollectionToCreate("collection1");
        AddCollectionToCreate("collection2");
        AddCollectionForRename("oldCollection", "newCollection");
        AddCollectionToDrop("myCollection");
    }
}
```
Index Migration:
These are migrations performed on the database to create/drop indexes. Specify the collection name, version number (Semantic Versioning) and an optional description, then simply add to the appropriate migration property to create/drop index.
Only add to list dictionary which you need to migrate

```csharp
public class ProductIndexSetUp : IndexMigration
{
    public override Version Version => new Version(1, 1, 1, 1);
    public override string Description => "Product index setup";

    public override string CollectionName => "product";

    public override void Prepare()
    {
        AddIndex("name", SortOrder.Ascending);
        AddIndex("status", SortOrder.Descending);
        AddIndex("store.id", SortOrder.Ascending);
        AddIndex(new[] { "lastUpdatedUtc", "_id" },
            new[] { SortOrder.Ascending, SortOrder.Ascending });
        AddIndex(new[] { "_id", "lastUpdatedUtc" },
            new[] { SortOrder.Ascending, SortOrder.Ascending });
        
        AddExpiryIndex("lastUpdatedUtc", 30);

        AddToDropIndex("indexName1");
        AddToDropIndex("indexName2"); 
    }
}
```

Seeding Migration:
These are migrations performed on the collection to upload a document to the collection . Specify the collection name, version number (Semantic Versioning) and an optional description, then simply specify the list of the BsonDocuments to be uploaded.  

```csharp
public class InitializeCollection_BsonDocument : SeedingDataMigration<BsonDocument>
{
    public override Version Version => new Version(1, 1, 1, 3);
    public override string Description => "Upload documents in collection";

    public override string CollectionName => "items";

    public override void Prepare()
    {
        var document = GetBsonDocument();

        Seed(document);
    }

    private BsonDocument GetBsonDocument()
    {
        return new BsonDocument {
            { "Type", "product" },
            { "ProductName", "Books" },
            {
                "Store",
                new BsonDocument { { "Id", "1" }, { "Country", "UK" } }
            },
            {
                "Sales",
                new BsonArray {20, 30, 40}
            },
            {
                "TargetGroup",
                new BsonArray {
                    new BsonDocument { { "Buyer", "Youngsters" }, { "SellingPitch", "Fiction" } },
                    new BsonDocument { { "Buyer", "Working Professional" }, { "SellingPitch", "Work Life Balance" } }
                }
            },
            { "Rating", "5*" }
        };
    }
}

public class InitializeCollection_Item : SeedingDataMigration<Item>
{
    public override Version Version => new Version(1, 1, 1, 4);
    public override string Description => "Upload documents in collection";

    public override string CollectionName => "items";

    public override void Prepare()
    {
        var productDocument = GetItem();

        Seed(productDocument);
    }

    private Item GetItem()
    {
        return new Item
        {
            Type = "product",
            ProductName = "Stationary",
            Sales = new[] { 100, 127, 167 },
            TargetGroup = new[]
            {
                new TargetGroup
                {
                    Buyer = "School Kids",
                    SellingPitch = "Safe Colorful Material"
                },
                new TargetGroup
                {
                    Buyer = "Working Professional",
                    SellingPitch = "Durable Material"
                }
            }
        };
    }
}
```

Document Migration:
These are migrations performed on the documents of the collection to update the documents to add a new field or replace value of an existing field from the value of an existing field or some static value. DocumentMigration has two types: 
1. ServerSideDocumentMigration 
2. ClientSideDocumentMigration

Server Side Document Migration:
In this migration you can specify a static value or provide an expression to apply some calculation/method(concat/sum/average) on the values before assigning it to the new field. The update is done via aggregation pipeline so you can also specify your own aggregation pipeline apart from $set/$addfield in the migration. You can also specify the filters on which the update should be applied. If not specified, it will be by default applied to all documents in the collection.   

```csharp
public class DocumentsUpdate_Revision4 : ServerSideDocumentMigration
{
    public override Version Version => new Version(1, 1, 1, 4);
    public override string Description => "documents update to new schema";
    public override string CollectionName => "items";

    public override void Prepare()
    {
        AddMigrationField("ProductDetails", "{ $concat: [ \"$Type\", \" - \", \"$ProductName\" ] }");
        AddMigrationField("ProductType", "\"$Type\"");
        AddMigrationField("Store.Region", "{ $concat: [ \"North \", \"$Store.Country\" ] }");
            AddMigrationField("Sales", "{ $concatArrays: [ \"$Sales\", [ 55 ] ] }");
            AddMigrationField("Ratings", "[ \"A\", \"B\" , \"$Rating\" ]");
    }
}

public class DocumentsUpdate_Revision7 : ServerSideDocumentMigration
{
    public override Version Version => new Version(1, 1, 1, 7);
    public override string Description => "documents update to new schema";
    public override string CollectionName => "items";
    public override FilterDefinition<BsonDocument> Filters => BuildFilters();
    public override PipelineDefinition<BsonDocument, BsonDocument> PipelineDefinition { get; set; }

    public override void Prepare()
    {
        PipelineDefinition = BuildPipelineDefinition();
    }

    private static FilterDefinition<BsonDocument> BuildFilters()
    {
        var filterBuilder = new FilterDefinitionBuilder<BsonDocument>();
        var idFilter = filterBuilder.Eq("ProductName", "Books");
        var filter = filterBuilder.And(idFilter);
        return filter;
    }
    
    private static PipelineDefinition<BsonDocument, BsonDocument> BuildPipelineDefinition()
    {
        var pipeline = new EmptyPipelineDefinition<BsonDocument>()
            .AppendStage("{ $addFields: { \"TargetGroup\": { \"$map\": { \"input\": \"$TargetGroup\", \"as\": \"row\", \"in\": { \"Buyer\": \"$$row.Buyer\", \"SellingPitch\": \"$$row.SellingPitch\"," + 
                            " \"Genre\": \"$$row.SellingPitch\"  } } }}}",
                BsonDocumentSerializer.Instance)
            .AppendStage("{ $unset: \"Rating\" }", BsonDocumentSerializer.Instance);
        return pipeline;
    }
}
```

Client Side Documnet Migration:
This migration can be used if the calculation of the new field value is somewhat complex. This migration is applied by looping through each of the document in the collection.  You can also specify the filters on which the migration should be applied. If not specified, it will be by default applied to all documents in the collection. You need to override the MigrateDocument method to restructure the document. The returned restructured document is then replaced in the collection. You can specify the field which should be used in the replace method filter. If not specified the default filter of _id will be used. By default all the documents are loaded in memory at once but you can override BatchSize property to specify the chunks in which you wish to apply the migration.  

```csharp
public class ClientSideUpdateDocument : ClientSideDocumentMigration
{
    public override Version Version => new Version(1, 1, 1, 9);
    public override string Description => "Upload documents in collection by restructuring document in client code";
    public virtual int BatchSize { get; } = 2;
    public override string CollectionName => "items";

    public override void Prepare()
    {
        //No preparation required
    }

    public override BsonDocument MigrateDocument(BsonDocument document)
    {
        document.AsBsonDocument.TryGetElement("TargetGroup", out var element);
        var bsonValue = element.Value;
        var updatedValues = new List<string>();
        if (bsonValue.IsBsonArray)
        {
            var array = bsonValue.AsBsonArray;
            foreach (var arrayElement in array)
            {
                arrayElement.AsBsonDocument.TryGetElement("Buyer", out var buyer);
                arrayElement.AsBsonDocument.TryGetElement("SellingPitch", out var sellingPitch);
                var newValue = $"{buyer.Value} - {sellingPitch.Value}";
                updatedValues.Add(newValue);
            }
        }
        document.Set("NewTargetGroup", ToBsonDocumentArray(updatedValues));
        return document;
    }

    public static BsonArray ToBsonDocumentArray(List<string> itemList)
    {
        var array = new BsonArray();
        foreach (var item in itemList)
        {
            array.Add(item);
        }
        return array;
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
If for any reason the migration fails then that is marked as Errored in DB. However if you wish to rerun the same migration the override the RerunMigration property

```csharp
public class InitializeCollection : SeedingDataMigration<Item>
{
    public override Version Version => new Version(1, 1, 1, 7);
    public override string Description => "Upload documents in collection";
    public override bool RerunMigration => true;
    .....
}
```
