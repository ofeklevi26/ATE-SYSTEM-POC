using System.Threading;
using System.Threading.Tasks;

namespace Ate.Engine.Wrappers;

public interface IPsuControl
{
    Task<object> SetOutputAsync(bool enabled = true, int? channel = null, CancellationToken token = default);
}

public interface INiDaqMxControl
{
    Task<object> SetContiniousFrequencyAsync(
        decimal frequency,
        decimal dutyCycle,
        bool isIdleStateHugh = false,
        int? channel = null,
        CancellationToken token = default);
}
