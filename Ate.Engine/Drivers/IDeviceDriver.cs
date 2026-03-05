using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Contracts;

namespace Ate.Engine.Drivers;

public interface IDeviceDriver
{
    string DeviceType { get; }

    string DriverId { get; }

    DeviceCommandDefinition GetCommandDefinition();

    Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token);
}
