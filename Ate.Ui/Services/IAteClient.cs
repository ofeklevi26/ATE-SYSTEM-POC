using System.Collections.Generic;
using System.Threading.Tasks;
using Ate.Contracts;

namespace Ate.Ui.Services;

public interface IAteClient
{
    Task<DeviceCommandResponse?> SendCommandAsync(DeviceCommandRequest request);

    Task<EngineStatus?> GetStatusAsync();

    Task<List<DeviceCommandDefinition>?> GetCapabilitiesAsync();

    Task PauseAsync();

    Task ResumeAsync();

    Task ClearAsync();

    Task AbortCurrentAsync();
}
