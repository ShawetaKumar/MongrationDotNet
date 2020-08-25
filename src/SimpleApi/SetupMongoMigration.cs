using System.Threading;
using System.Threading.Tasks;
using MongrationDotNet;

namespace SimpleApi
{
    public class SetupMongoMigration : IStartupTask
    {
        private readonly IMigrationRunner migrationRunner;

        public SetupMongoMigration(IMigrationRunner migrationRunner)
        {
            this.migrationRunner = migrationRunner;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await migrationRunner.Migrate();
        }
    }
}