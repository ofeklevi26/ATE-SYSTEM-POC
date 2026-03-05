using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using Ate.Contracts;
using Ate.Ui.Services;

namespace Ate.Ui.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AteClient _client = new AteClient();
    private readonly DispatcherTimer _timer;
    private string _deviceType = "DMM";
    private string _operation = "MeasureVoltage";
    private string _parametersJson = "{}";
    private string _statusText = "Disconnected";

    public MainViewModel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, __) => await RefreshStatusAsync();
        _timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DeviceType
    {
        get => _deviceType;
        set => SetField(ref _deviceType, value);
    }

    public string Operation
    {
        get => _operation;
        set => SetField(ref _operation, value);
    }

    public string ParametersJson
    {
        get => _parametersJson;
        set => SetField(ref _parametersJson, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public async Task SendAsync()
    {
        try
        {
            var request = new DeviceCommandRequest
            {
                DeviceType = DeviceType,
                Operation = Operation,
                Parameters = ParseParameters(ParametersJson),
                ClientRequestId = Guid.NewGuid().ToString("N")
            };

            var response = await _client.SendCommandAsync(request);
            StatusText = $"Enqueued: {response?.ServerCommandId}";
        }
        catch (Exception ex)
        {
            StatusText = $"Send failed: {ex.Message}";
        }
    }

    public Task PauseAsync() => _client.PauseAsync();

    public Task ResumeAsync() => _client.ResumeAsync();

    public Task ClearAsync() => _client.ClearAsync();

    public Task AbortAsync() => _client.AbortCurrentAsync();

    public async Task RefreshStatusAsync()
    {
        try
        {
            var status = await _client.GetStatusAsync();
            if (status == null)
            {
                StatusText = "No status response.";
                return;
            }

            StatusText =
                $"State={status.State}, Queue={status.QueueLength}, Current={status.CurrentCommand ?? "n/a"}, LastError={status.LastError ?? "none"}, Drivers=[{string.Join(",", status.LoadedDrivers)}]";
        }
        catch
        {
            StatusText = "Engine unreachable.";
        }
    }

    private static Dictionary<string, object> ParseParameters(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? memberName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
}
