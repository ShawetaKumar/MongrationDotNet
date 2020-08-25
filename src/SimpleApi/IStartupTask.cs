using System.Threading;
using System.Threading.Tasks;

namespace SimpleApi
{
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}