using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.DemoDrivers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class NiDaqMxDeviceWrapper : IDeviceDriver
{
    private readonly INiDaqMxDriverAdapter _adapter;

    public NiDaqMxDeviceWrapper(string driverId, string card_number, string endpoint = "")
    {
        DriverId = driverId;
        CardNumber = card_number;
        Endpoint = endpoint;

        var builder = new NiDaqMxHardwareDriverBuilder(endpoint);
        _adapter = builder.BuildDaqMxDriverAdapter();
    }

    public string DeviceType => "NiDaqMx";

    public string DriverId { get; }

    public string CardNumber { get; }

    public string Endpoint { get; }

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
    public object SetContiniousFrequency(decimal frequency, decimal dutyCycle, bool isIdleStateHugh = false, int? channel = null)
    {
        var selectedChannel = channel ?? 1;
        var status = _adapter.SetContiniousFrequency(selectedChannel, frequency, dutyCycle, isIdleStateHugh);
        return new
        {
            CardNumber,
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
        var selectedChannel = channel ?? 1;
        return $"{CardNumber}:{_adapter.Identify(selectedChannel)}";
    }
}
