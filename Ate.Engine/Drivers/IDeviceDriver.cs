using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ate.Engine.Drivers;

public interface IDeviceDriver
{
    string DeviceType { get; }

    Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token);
}
