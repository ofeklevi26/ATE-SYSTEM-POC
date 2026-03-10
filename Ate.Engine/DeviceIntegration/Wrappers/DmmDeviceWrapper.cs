using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class DmmDeviceWrapper : IDeviceDriver
{
    private readonly IDmmHardwareDriver _hardware;

    public DmmDeviceWrapper(string driverId, string ip, int channel, string endpoint, IDmmHardwareDriver hardware)
    {
        DriverId = driverId;
        Ip = ip;
        Channel = channel;
        Endpoint = endpoint;
        _hardware = hardware;
    }

    public string DeviceType => "DMM";

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
    public object MeasureVoltage(decimal range = 10.0m, int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        var value = _hardware.MeasureVoltage(Ip, selectedChannel, range);
        return new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" };
    }

    [DriverOperation]
    public object Identify(int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        return _hardware.Identify(Ip, selectedChannel);
    }
}
