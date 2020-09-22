namespace MongrationDotNet
{
    public class MigrationConcurrencyOptions
    {
        public int WaitInterval { get; set; } = 5000;
        public int TimeoutCount { get; set; } = 25;
    }
}