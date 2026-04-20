using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;

namespace Ate.Engine.Wrappers;

public sealed class MeditationDeviceWrapper : IDeviceDriver
{
    private readonly DriverRegistry _driverRegistry;
    private readonly string _psuWrapperName;
    private readonly string _niDaqMxWrapperName;
    private IPsuControl? _psuControl;
    private INiDaqMxControl? _niDaqMxControl;

    public MeditationDeviceWrapper(
        string driverId,
        DriverRegistry driverRegistry,
        string psuWrapperName = "PSU",
        string niDaqMxWrapperName = "NiDaqMx",
        int ledChannel = 1,
        int buzzerChannel = 1,
        decimal buzzerFrequency = 1000.0m,
        decimal buzzerDutyCycle = 50.0m)
    {
        DriverId = driverId;
        _driverRegistry = driverRegistry;
        _psuWrapperName = psuWrapperName;
        _niDaqMxWrapperName = niDaqMxWrapperName;
        LedChannel = ledChannel;
        BuzzerChannel = buzzerChannel;
        BuzzerFrequency = buzzerFrequency;
        BuzzerDutyCycle = buzzerDutyCycle;
    }

    public string DeviceType => "Meditation";

    public string DriverId { get; }

    public int LedChannel { get; }

    public int BuzzerChannel { get; }

    public decimal BuzzerFrequency { get; }

    public decimal BuzzerDutyCycle { get; }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return WrapperOperationRuntime.InvokeAsync(this, operation, parameters, token);
    }

    [DriverOperation]
    public async Task<object> start_buzzer_sequence()
    {
        var psuControl = ResolvePsuControl();
        var niDaqMxControl = ResolveNiDaqMxControl();

        var turnOnResult = await psuControl
            .SetOutputAsync(enabled: true, channel: LedChannel)
            .ConfigureAwait(false);

        var buzzerResult = await niDaqMxControl
            .SetContiniousFrequencyAsync(
                frequency: BuzzerFrequency,
                dutyCycle: BuzzerDutyCycle,
                isIdleStateHugh: false,
                channel: BuzzerChannel)
            .ConfigureAwait(false);

        var turnOffResult = await psuControl
            .SetOutputAsync(enabled: false, channel: LedChannel)
            .ConfigureAwait(false);

        return new
        {
            Wrapper = DeviceType,
            Action = "start_buzzer_sequence",
            LedChannel,
            BuzzerChannel,
            BuzzerFrequency,
            BuzzerDutyCycle,
            TurnOnResult = turnOnResult,
            BuzzerResult = buzzerResult,
            TurnOffResult = turnOffResult
        };
    }

    private IPsuControl ResolvePsuControl()
    {
        _psuControl ??= ResolveWrapper<IPsuControl>("PSU", _psuWrapperName);
        return _psuControl;
    }

    private INiDaqMxControl ResolveNiDaqMxControl()
    {
        _niDaqMxControl ??= ResolveWrapper<INiDaqMxControl>("NiDaqMx", _niDaqMxWrapperName);
        return _niDaqMxControl;
    }

    private TWrapper ResolveWrapper<TWrapper>(string deviceType, string wrapperName)
        where TWrapper : class
    {
        if (!_driverRegistry.TryResolve(deviceType, wrapperName, out var wrapper) || wrapper == null)
        {
            throw new InvalidOperationException(
                $"Meditation wrapper dependency not found: deviceType='{deviceType}', wrapperName='{wrapperName}'.");
        }

        if (wrapper is not TWrapper typedWrapper)
        {
            throw new InvalidOperationException(
                $"Meditation wrapper dependency '{deviceType}::{wrapperName}' does not implement required contract '{typeof(TWrapper).Name}'.");
        }

        return typedWrapper;
    }
}
