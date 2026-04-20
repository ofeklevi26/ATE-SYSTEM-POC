using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;

namespace Ate.Engine.Wrappers;

public sealed class MeditationDeviceWrapper : IDeviceDriver
{
    private readonly DriverRegistry _driverRegistry;
    private IDeviceDriver? _psuWrapper;
    private IDeviceDriver? _niDaqMxWrapper;

    public MeditationDeviceWrapper(
        string driverId,
        DriverRegistry driverRegistry,
        int ledChannel = 1,
        int buzzerChannel = 1,
        decimal buzzerFrequency = 1000.0m,
        decimal buzzerDutyCycle = 50.0m)
    {
        DriverId = driverId;
        _driverRegistry = driverRegistry;
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
        var turnOnResult = await InvokePsuAsync("SetOutput", new Dictionary<string, object>
        {
            ["enabled"] = true,
            ["channel"] = LedChannel
        }).ConfigureAwait(false);

        var buzzerResult = await InvokeNiDaqAsync("SetContiniousFrequency", new Dictionary<string, object>
        {
            ["frequency"] = BuzzerFrequency,
            ["dutyCycle"] = BuzzerDutyCycle,
            ["isIdleStateHugh"] = false,
            ["channel"] = BuzzerChannel
        }).ConfigureAwait(false);

        var turnOffResult = await InvokePsuAsync("SetOutput", new Dictionary<string, object>
        {
            ["enabled"] = false,
            ["channel"] = LedChannel
        }).ConfigureAwait(false);

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

    private Task<object> InvokePsuAsync(string operation, Dictionary<string, object> parameters)
    {
        _psuWrapper ??= ResolveFirstWrapper("PSU");
        return _psuWrapper.ExecuteAsync(operation, parameters, CancellationToken.None);
    }

    private Task<object> InvokeNiDaqAsync(string operation, Dictionary<string, object> parameters)
    {
        _niDaqMxWrapper ??= ResolveFirstWrapper("NiDaqMx");
        return _niDaqMxWrapper.ExecuteAsync(operation, parameters, CancellationToken.None);
    }

    private IDeviceDriver ResolveFirstWrapper(string deviceType)
    {
        var firstMatchingKey = _driverRegistry.GetLoadedDrivers()
            .FirstOrDefault(k => k.StartsWith(deviceType + "::", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(firstMatchingKey))
        {
            throw new InvalidOperationException(
                $"Meditation wrapper dependency not found: no registered wrapper for deviceType='{deviceType}'.");
        }

        var split = firstMatchingKey.Split(new[] { "::" }, StringSplitOptions.None);
        if (split.Length != 2 || !_driverRegistry.TryResolve(split[0], split[1], out var resolvedDriver) || resolvedDriver == null)
        {
            throw new InvalidOperationException(
                $"Meditation wrapper dependency could not be resolved for key '{firstMatchingKey}'.");
        }

        return resolvedDriver;
    }
}
