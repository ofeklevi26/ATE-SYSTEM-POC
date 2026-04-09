using System;
using System.Collections.Generic;
using System.Globalization;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.DemoDrivers;

public sealed class NiDaqMxHardwareDriverBuilder : INiDaqMxDriverBuilder
{
    public INiDaqMxDriverAdapter BuildDaqMxDriverAdapter(string deviceName, ILogger? logger = null)
    {
        return new NiDaqMxHardwareDriverAdapter(deviceName, logger);
    }
}

public sealed class NiDaqMxHardwareDriverAdapter : INiDaqMxDriverAdapter
{
    private readonly Dictionary<int, ChannelWaveformState> _channelState = new();
    private readonly string _deviceName;
    private readonly ILogger? _logger;
    private bool _connected;

    public NiDaqMxHardwareDriverAdapter(string deviceName, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new InvalidOperationException("NI-DAQmx device name is required.");
        }

        _deviceName = deviceName.Trim();
        _logger = logger;
    }

    public void Connect()
    {
        if (!_deviceName.Contains("Dev", StringComparison.OrdinalIgnoreCase) && !_deviceName.Contains("slot", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invalid NI-DAQmx device name '{_deviceName}'. Expected a value like 'Dev9220' or 'NI_9444_slot1'.");
        }

        _connected = true;
        _logger?.Info($"NI-DAQmx adapter connected to {_deviceName}.");
    }

    public void Disconnect()
    {
        _connected = false;
    }

    public string Identify(string address, int channel)
    {
        var target = RequireConnection();
        _ = address;
        return $"NI-DAQmx PWM@{target}:CH{channel}";
    }

    public string SetContiniousFrequency(string address, int channel, decimal frequency, decimal dutyCycle, bool isIdleStateHugh)
    {
        _ = address;
        var target = RequireConnection();

        if (frequency <= 0)
        {
            throw new InvalidOperationException("Frequency must be greater than zero.");
        }

        if (dutyCycle < 0 || dutyCycle > 100)
        {
            throw new InvalidOperationException("Duty cycle must be between 0 and 100.");
        }

        var state = new ChannelWaveformState(frequency, dutyCycle, isIdleStateHugh);
        _channelState[channel] = state;

        return string.Format(
            CultureInfo.InvariantCulture,
            "Configured {0}:CH{1} => Frequency={2}Hz, DutyCycle={3}%, IdleStateHigh={4}",
            target,
            channel,
            state.Frequency,
            state.DutyCycle,
            state.IsIdleStateHugh);
    }

    private string RequireConnection()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("No NI-DAQmx adapter connection is active. Build an adapter and call Connect first.");
        }

        return _deviceName;
    }

    private sealed class ChannelWaveformState
    {
        public ChannelWaveformState(decimal frequency, decimal dutyCycle, bool isIdleStateHugh)
        {
            Frequency = frequency;
            DutyCycle = dutyCycle;
            IsIdleStateHugh = isIdleStateHugh;
        }

        public decimal Frequency { get; }

        public decimal DutyCycle { get; }

        public bool IsIdleStateHugh { get; }
    }
}
