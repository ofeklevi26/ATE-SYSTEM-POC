using System;
using System.Collections.Generic;
using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

/// <summary>
/// Standalone PSU hardware driver sample that simulates what a real vendor SDK adapter would look like.
/// This file is intentionally not wired into DI/module/config so you can integrate it yourself.
/// </summary>
public sealed class SimulatedVendorPsuHardwareDriver : IPsuHardwareDriver
{
    private readonly Dictionary<int, ChannelState> _channels = new();
    private bool _connected;
    private string _target = string.Empty;

    public void Connect(string connectionTarget)
    {
        if (string.IsNullOrWhiteSpace(connectionTarget))
        {
            throw new ArgumentException("Connection target is required.", nameof(connectionTarget));
        }

        _target = connectionTarget.Trim();
        _connected = true;
    }

    public void Disconnect()
    {
        _connected = false;
    }

    public string Identify(string address, int channel)
    {
        EnsureConnected();
        return $"SIM-VENDOR-PSU@{address}:CH{channel} via {_target}";
    }

    public void SetVoltage(int channel, decimal voltage, decimal currentLimit)
    {
        EnsureConnected();
        var state = GetOrCreate(channel);
        state.Voltage = voltage;
        state.CurrentLimit = currentLimit;
    }

    public void SetCurrentLimit(int channel, decimal currentLimit)
    {
        EnsureConnected();
        var state = GetOrCreate(channel);
        state.CurrentLimit = currentLimit;
    }

    public void SetOutput(int channel, bool enabled)
    {
        EnsureConnected();
        var state = GetOrCreate(channel);
        state.OutputEnabled = enabled;
    }

    private ChannelState GetOrCreate(int channel)
    {
        if (!_channels.TryGetValue(channel, out var state))
        {
            state = new ChannelState();
            _channels[channel] = state;
        }

        return state;
    }

    private void EnsureConnected()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("Driver is not connected. Call Connect(connectionTarget) first.");
        }
    }

    private sealed class ChannelState
    {
        public decimal Voltage { get; set; }

        public decimal CurrentLimit { get; set; }

        public bool OutputEnabled { get; set; }
    }
}
