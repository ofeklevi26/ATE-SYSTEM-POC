using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class DmmDeviceWrapper : IDeviceDriver
{
    private readonly IDmmHardwareDriver _hardware;

    public DmmDeviceWrapper(string driverId, string address, int channel, string endpoint, IDmmHardwareDriver hardware)
    {
        DriverId = driverId;
        Address = address;
        Channel = channel;
        Endpoint = endpoint;
        _hardware = hardware;
    }

    public string DeviceType => "DMM";

    public string DriverId { get; }

    public string Address { get; }

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
    public object MeasureVoltage(decimal range = 10.0m, int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        var value = _hardware.MeasureVoltage(Address, selectedChannel, range);
        return new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" };
    }

    [DriverOperation]
    public object Identify(int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        return _hardware.Identify(Address, selectedChannel);
    }
}
