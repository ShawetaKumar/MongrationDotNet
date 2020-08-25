using System.Collections.Generic;

namespace MongrationDotNet
{
    public static class DictionaryExtensions
    {
        public static void AddRenameProperty(this Dictionary<string, Dictionary<string, string>> migrationObjects,
            string collection, string from, string to)
        {
            if (!migrationObjects.ContainsKey(collection))
                migrationObjects.Add(collection, new Dictionary<string, string> {{from, to}});
            else
            {
                var value = migrationObjects.GetValueOrDefault(collection);
                value.Add(from, to);
                migrationObjects[collection] = value;
            }
        }

        public static void AddRemoveProperty(this Dictionary<string, Dictionary<string, string>> migrationObjects,
            string collection, string field)
        {
            if (!migrationObjects.ContainsKey(collection))
                migrationObjects.Add(collection, new Dictionary<string, string> {{field, string.Empty}});
            else
            {
                var value = migrationObjects.GetValueOrDefault(collection);
                value.Add(field, string.Empty);
                migrationObjects[collection] = value;
            }
        }

        public static void AddToList(this Dictionary<string, ICollection<Dictionary<string, SortOrder>>> dictionary,
            string collection, string field, SortOrder sortOrder)
        {
            if (!dictionary.ContainsKey(collection))
                dictionary.Add(collection,
                    new List<Dictionary<string, SortOrder>> {new Dictionary<string, SortOrder> {{field, sortOrder}}});
            else
            {
                var value = dictionary.GetValueOrDefault(collection);
                value.Add(new Dictionary<string, SortOrder> {{field, sortOrder}});
                dictionary[collection] = value;
            }
        }

        public static void AddToList(this Dictionary<string, ICollection<Dictionary<string, SortOrder>>> dictionary,
            string collection, string[] fields, SortOrder[] sortOrder)
        {
            var indexCombinations = new Dictionary<string, SortOrder>();
            for (var i = 0; i < fields.Length; i++)
            {
                indexCombinations.Add(fields[i], sortOrder[i]);
            }

            if (!dictionary.ContainsKey(collection))
                dictionary.Add(collection, new[] {indexCombinations});
            else
            {
                var value = dictionary.GetValueOrDefault(collection);
                value.Add(indexCombinations);
                dictionary[collection] = value;
            }
        }

        public static void AddToList(this Dictionary<string, ICollection<string>> dictionary, string collection,
            ICollection<string> indexes)
        {
            if (!dictionary.ContainsKey(collection))
                dictionary.Add(collection, indexes);
        }

        public static void AddToList(this Dictionary<string, Dictionary<string, int>> dictionary, string collection,
            string field, int days)
        {
            if (!dictionary.ContainsKey(collection))
                dictionary.Add(collection, new Dictionary<string, int> {{field, days}});
            else
            {
                var value = dictionary.GetValueOrDefault(collection);
                if (value.ContainsKey(field)) return;
                value.Add(field, days);
                dictionary[collection] = value;
            }
        }

        public static void AddToList(this Dictionary<string, string> dictionary, string from, string to)
        {
            if (!dictionary.ContainsKey(from))
                dictionary.Add(from, to);
        }
    }
}