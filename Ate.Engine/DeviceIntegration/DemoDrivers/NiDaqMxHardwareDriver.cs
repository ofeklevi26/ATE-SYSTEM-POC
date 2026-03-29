using System;
using System.Collections.Generic;
using System.Globalization;
using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class NiDaqMxHardwareDriver : INiDaqMxHardwareDriver
{
    private readonly Dictionary<int, ChannelWaveformState> _channelState = new();
    private string? _connectionTarget;

    public void Connect(string connectionTarget)
    {
        if (string.IsNullOrWhiteSpace(connectionTarget))
        {
            throw new InvalidOperationException("NI-DAQmx connection target is required.");
        }

        if (!connectionTarget.Contains("_slot", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invalid NI-DAQmx connection target '{connectionTarget}'. Expected format like 'NI_9444_slot1'.");
        }

        _connectionTarget = connectionTarget.Trim();
    }

    public void Disconnect()
    {
        _connectionTarget = null;
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
        if (string.IsNullOrWhiteSpace(_connectionTarget))
        {
            throw new InvalidOperationException("No NI-DAQmx connection is active. Call Connect first.");
        }

        return _connectionTarget;
    }

    private sealed record ChannelWaveformState(decimal Frequency, decimal DutyCycle, bool IsIdleStateHugh);
}
