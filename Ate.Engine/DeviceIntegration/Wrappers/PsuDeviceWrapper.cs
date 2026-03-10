using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class PsuDeviceWrapper : IDeviceDriver
{
    private readonly IPsuHardwareDriver _hardware;

    public PsuDeviceWrapper(string driverId, string ip, int channel, string endpoint, IPsuHardwareDriver hardware)
    {
        DriverId = driverId;
        Ip = ip;
        Channel = channel;
        Endpoint = endpoint;
        _hardware = hardware;
    }

    public string DeviceType => "PSU";

    public string DriverId { get; }

    public string Ip { get; }

    public int Channel { get; }

    public string Endpoint { get; }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _hardware.Connect(Endpoint);
        try
        {
            return WrapperOperationRuntime.InvokeAsync(this, operation, parameters, token);
        }
        finally
        {
            _hardware.Disconnect();
        }
    }

    [DriverOperation]
    public object Identify(int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        return _hardware.Identify(Ip, selectedChannel);
    }

    [DriverOperation]
    public object SetVoltage(decimal voltage, decimal currentLimit = 1.0m, int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        _hardware.SetVoltage(selectedChannel, voltage, currentLimit);
        return $"PSU configured: Voltage={voltage.ToString("0.###", CultureInfo.InvariantCulture)}V, CurrentLimit={currentLimit.ToString("0.###", CultureInfo.InvariantCulture)}A on CH{selectedChannel}";
    }

    [DriverOperation]
    public object SetCurrentLimit(decimal currentLimit, int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        _hardware.SetCurrentLimit(selectedChannel, currentLimit);
        return $"PSU current limit set to {currentLimit.ToString("0.###", CultureInfo.InvariantCulture)} A on CH{selectedChannel}";
    }

    [DriverOperation]
    public object SetOutput(bool enabled = true, int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        _hardware.SetOutput(selectedChannel, enabled);
        return enabled ? $"PSU output enabled on CH{selectedChannel}" : $"PSU output disabled on CH{selectedChannel}";
    }

    [DriverOperation]
    public object OutputOff(int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        _hardware.SetOutput(selectedChannel, false);
        return $"PSU output disabled on CH{selectedChannel}";
    }
}
