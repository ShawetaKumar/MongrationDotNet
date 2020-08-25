using System.Collections.Generic;

namespace MongrationDotNet
{
    public static class CollectionExtensions
    {
        public static void AddRenameProperty(this ICollection<(string, string)> migrationFields, string from, string to)
        {
            migrationFields.Add((from, to));
        }

        public static void AddRemoveProperty(this ICollection<(string, string)> migrationFields, string field)
        {
            migrationFields.Add((field, string.Empty));
        }
    }
}