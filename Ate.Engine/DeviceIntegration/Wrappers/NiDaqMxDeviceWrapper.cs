using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class NiDaqMxDeviceWrapper : IDeviceDriver
{
    private readonly INiDaqMxHardwareDriver _hardware;

    public NiDaqMxDeviceWrapper(string driverId, string address, int channel, string endpoint, INiDaqMxHardwareDriver hardware)
    {
        DriverId = driverId;
        Address = address;
        Channel = channel;
        Endpoint = endpoint;
        _hardware = hardware;
    }

    public string DeviceType => "NiDaqMx";

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
    public object SetContiniousFrequency(decimal frequency, decimal dutyCycle, bool isIdleStateHugh = false, int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        var status = _hardware.SetContiniousFrequency(Address, selectedChannel, frequency, dutyCycle, isIdleStateHugh);
        return new
        {
            Channel = selectedChannel,
            Frequency = frequency,
            DutyCycle = dutyCycle,
            IsIdleStateHugh = isIdleStateHugh,
            Status = status
        };
    }

    [DriverOperation]
    public object Identify(int? channel = null)
    {
        var selectedChannel = channel ?? Channel;
        return _hardware.Identify(Address, selectedChannel);
    }
}
