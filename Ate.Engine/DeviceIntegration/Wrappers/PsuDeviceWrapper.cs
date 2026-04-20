using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.DemoDrivers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class PsuDeviceWrapper : IDeviceDriver
{
    private readonly IPsuDriverAdapter _adapter;

    public PsuDeviceWrapper(string driverId, string address, string port = "", string endpoint = "")
    {
        DriverId = driverId;
        Address = address;
        Port = port;
        Endpoint = endpoint;

        var builder = new DemoPsuHardwareDriverBuilder(endpoint);
        _adapter = builder.BuildPsuDriverAdapter();
    }

    public string DeviceType => "PSU";

    public string DriverId { get; }

    public string Address { get; }

    public string Port { get; }

    public string Endpoint { get; }

    public void Connect()
    {
        _adapter.Connect();
    }

    public void Disconnect()
    {
        _adapter.Disconnect();
    }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _adapter.Connect();

        try
        {
            return WrapperOperationRuntime.InvokeAsync(this, operation, parameters, token);
        }
        finally
        {
            _adapter.Disconnect();
        }
    }

    [DriverOperation]
    public object Identify(int? channel = null)
    {
        var selectedChannel = channel ?? 1;
        return _adapter.Identify(AddressWithPort(), selectedChannel);
    }

    [DriverOperation]
    public object SetVoltage(decimal voltage, decimal currentLimit = 1.0m, int? channel = null)
    {
        var selectedChannel = channel ?? 1;
        _adapter.SetVoltage(selectedChannel, voltage, currentLimit);
        return $"PSU configured [{AddressWithPort()}]: Voltage={voltage.ToString("0.###", CultureInfo.InvariantCulture)}V, CurrentLimit={currentLimit.ToString("0.###", CultureInfo.InvariantCulture)}A on CH{selectedChannel}";
    }

    [DriverOperation]
    public object SetCurrentLimit(decimal currentLimit, int? channel = null)
    {
        var selectedChannel = channel ?? 1;
        _adapter.SetCurrentLimit(selectedChannel, currentLimit);
        return $"PSU current limit [{AddressWithPort()}] set to {currentLimit.ToString("0.###", CultureInfo.InvariantCulture)} A on CH{selectedChannel}";
    }

    [DriverOperation]
    public object SetOutput(bool enabled = true, int? channel = null)
    {
        var selectedChannel = channel ?? 1;
        _adapter.SetOutput(selectedChannel, enabled);
        return enabled ? $"PSU output enabled [{AddressWithPort()}] on CH{selectedChannel}" : $"PSU output disabled [{AddressWithPort()}] on CH{selectedChannel}";
    }

    [DriverOperation]
    public object OutputOff(int? channel = null)
    {
        var selectedChannel = channel ?? 1;
        _adapter.SetOutput(selectedChannel, false);
        return $"PSU output disabled [{AddressWithPort()}] on CH{selectedChannel}";
    }

    private string AddressWithPort()
    {
        return string.IsNullOrWhiteSpace(Port) ? Address : $"{Address}:{Port}";
    }
}
