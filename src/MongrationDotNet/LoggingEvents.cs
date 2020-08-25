using Microsoft.Extensions.Logging;

namespace MongrationDotNet
{
    public static class LoggingEvents
    {
        // information events
        public static EventId MigrationStarted = new EventId(0, nameof(MigrationStarted));
        public static EventId DatabaseMigrationStarted = new EventId(0, nameof(DatabaseMigrationStarted));
        public static EventId CollectionMigrationStarted = new EventId(0, nameof(CollectionMigrationStarted));
        public static EventId IndexMigrationStarted = new EventId(0, nameof(IndexMigrationStarted));
        public static EventId SeedingDataMigrationStarted = new EventId(0, nameof(SeedingDataMigrationStarted));
        public static EventId ApplyingDatabaseMigration = new EventId(0, nameof(ApplyingDatabaseMigration));
        public static EventId ApplyingCollectionMigration = new EventId(0, nameof(ApplyingCollectionMigration));
        public static EventId ApplyingIndexMigration = new EventId(0, nameof(ApplyingIndexMigration));
        public static EventId MigrationSkipped = new EventId(0, nameof(MigrationSkipped));

        // events that represent success
        public static EventId MigrationCompleted = new EventId(0, nameof(MigrationCompleted));
        public static EventId DatabaseMigrationCompleted = new EventId(0, nameof(DatabaseMigrationCompleted));
        public static EventId CollectionMigrationCompleted = new EventId(0, nameof(CollectionMigrationCompleted));
        public static EventId IndexMigrationCompleted = new EventId(0, nameof(IndexMigrationCompleted));
        public static EventId SeedingDataMigrationCompleted = new EventId(0, nameof(SeedingDataMigrationCompleted));

        // events due to errors by the application or our dependencies
        public static EventId MigrationFailed = new EventId(0, nameof(MigrationFailed));
    }
}