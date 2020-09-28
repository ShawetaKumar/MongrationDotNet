using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongrationDotNet
{
    public interface IMigrationRunner
    {
        public Task<List<MigrationDetails>> Migrate();
    }
}