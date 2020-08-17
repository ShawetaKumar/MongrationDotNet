using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class DatabaseMigration : Migration
    {
        private IMongoDatabase database;
        public override string Type { get; } = Constants.DatabaseMigrationType;
        public ICollection<string> CollectionCreationList { get; } = new List<string>();
        public ICollection<string> CollectionDropList { get; } = new List<string>();
        public Dictionary<string, string> CollectionRenameList { get; } = new Dictionary<string, string>();

        public Dictionary<string, ICollection<Dictionary<string, SortOrder>>> CreateIndexList { get; } =
            new Dictionary<string, ICollection<Dictionary<string, SortOrder>>>();

        public Dictionary<string, Dictionary<string, int>> CreateExpiryIndexList { get; } =
            new Dictionary<string, Dictionary<string, int>>();

        public Dictionary<string, ICollection<string>> DropIndexList { get; } =
            new Dictionary<string, ICollection<string>>();

        public override async Task ExecuteAsync(IMongoDatabase mongoDatabase)
        {
            database = mongoDatabase;

            foreach (var collectionName in CollectionCreationList)
            {
                await CreateCollection(collectionName);
            }

            foreach (var collectionName in CreateIndexList.Keys)
            {
                await CreateIndexes(collectionName,
                    CreateIndexList.GetValueOrDefault(collectionName));
            }

            foreach (var collectionName in CreateExpiryIndexList.Keys)
            {
                await CreateExpiryIndex(collectionName,
                    CreateExpiryIndexList.GetValueOrDefault(collectionName));
            }

            foreach (var collectionName in DropIndexList.Keys)
            {
                await DropIndexes(collectionName,
                    DropIndexList.GetValueOrDefault(collectionName));
            }

            foreach (var (from, to) in CollectionRenameList)
            {
                await RenameCollection(from, to);
            }

            foreach (var collectionName in CollectionDropList)
            {
                await database.DropCollectionAsync(collectionName);
            }
        }

        private async Task CreateCollection(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            if (!await collections.AnyAsync())
            {
                await database.CreateCollectionAsync(collectionName);
            }
        }

        private async Task RenameCollection(string from, string to)
        {
            if (!string.IsNullOrEmpty(to))
            {
                var filter = new BsonDocument("name", from);
                var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
                if (!await collections.AnyAsync())
                    return; 
                
                filter = new BsonDocument("name", to);
                collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
                if (!await collections.AnyAsync())
                    await database.RenameCollectionAsync(from, to);
            }
        }

        private async Task CreateIndexes(string collectionName, IEnumerable<IDictionary<string, SortOrder>> indexCombinations)
        {
            foreach (var indexOnFieldNames in indexCombinations)
            {
                var indexKeys = new BsonDocument(indexOnFieldNames.Select(x => new BsonElement(x.Key, x.Value)));
                var indexName =
                    $"{collectionName}_{string.Join("-", indexOnFieldNames.Select(x => $"{x.Key}({x.Value})"))}";
                await database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKeys,
                        new CreateIndexOptions { Name = indexName }));
            }
        }

        public async Task CreateExpiryIndex(string collectionName, Dictionary<string, int> indexCombinations)
        {
            foreach (var fieldName in indexCombinations.Keys)
            {
                var indexKey = new BsonDocument(fieldName, 1);
                var indexName = $"{collectionName}_{fieldName}";
                var collectionExpiryInDays = indexCombinations.GetValueOrDefault(fieldName);
                await database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(indexKey,
                        new CreateIndexOptions { Name = indexName, ExpireAfter = new TimeSpan(collectionExpiryInDays, 0, 0, 0) }));
            }
        }

        private async Task DropIndexes(string collectionName, ICollection<string> indexes)
        {
            if (!indexes.Any())
                await database.GetCollection<BsonDocument>(collectionName).Indexes.DropAllAsync();
            else
            {
                foreach (var index in indexes)
                {
                    await database.GetCollection<BsonDocument>(collectionName).Indexes.DropOneAsync(index);
                }
            }
        }

    }
}