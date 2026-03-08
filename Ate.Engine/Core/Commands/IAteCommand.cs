using System.Threading;
using System.Threading.Tasks;

namespace Ate.Engine.Commands;

public interface IAteCommand
{
    string Name { get; }

    Task ExecuteAsync(CancellationToken token);
}
