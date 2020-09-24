namespace MongrationDotNet
{
    public class MigrationOptions
    {
        public int MigrationProgressDbPollingInterval { get; set; } = 5000;
        public int MigrationProgressDbPollingTimeout { get; set; } = 150000;
    }
}