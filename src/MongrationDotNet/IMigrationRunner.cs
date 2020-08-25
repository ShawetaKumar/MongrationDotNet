using System.Threading.Tasks;

namespace MongrationDotNet
{
    public interface IMigrationRunner
    {
        public Task Migrate();
    }
}