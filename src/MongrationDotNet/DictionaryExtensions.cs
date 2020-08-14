using System.Collections.Generic;

namespace MongrationDotNet
{
    public static class DictionaryExtensions
    {
        public static void AddPropertyForRenameToMigration(this Dictionary<string, Dictionary<string, string>> migrationObjects, string collection, string from, string to)
        {
            if (!migrationObjects.ContainsKey(collection))
                migrationObjects.Add(collection, new Dictionary<string, string> { { from, to } });
            else
            {
                var value = migrationObjects.GetValueOrDefault(collection);
                value.Add(from, to);
                migrationObjects[collection] = value;
            }
        }
        public static void AddPropertyForRemovalToMigration(this Dictionary<string, Dictionary<string, string>> migrationObjects, string collection, string field)
        {
            if (!migrationObjects.ContainsKey(collection))
                migrationObjects.Add(collection, new Dictionary<string, string> { { field, string.Empty } });
            else
            {
                var value = migrationObjects.GetValueOrDefault(collection);
                value.Add(field, string.Empty);
                migrationObjects[collection] = value;
            }
                
        }
    }
}