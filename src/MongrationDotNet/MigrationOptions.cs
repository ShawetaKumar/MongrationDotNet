namespace MongrationDotNet
{
    public class MigrationOptions
    {
        /// <summary>
        /// Number of miliseconds a node will wait before polling for migration status if any migration is in progress on another node
        /// </summary>
        public int MigrationProgressDbPollingInterval { get; set; } = 5000;
        /// <summary>
        /// Number of miliseconds after which the node will timeout and suspend the migration process
        /// </summary>
        public int MigrationProgressDbPollingTimeout { get; set; } = 150000;
    }
}