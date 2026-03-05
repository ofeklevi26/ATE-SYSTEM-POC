using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using Ate.Contracts;
using Ate.Ui.Services;
using CommunityToolkit.Mvvm.Input;

namespace Ate.Ui.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AteClient _client = new AteClient();
    private readonly DispatcherTimer _timer;

    private DeviceCommandDefinition? _selectedDevice;
    private CommandOperationDefinition? _selectedOperation;
    private string _statusText = "Disconnected";

    public MainViewModel()
    {
        Devices = new ObservableCollection<DeviceCommandDefinition>();
        Operations = new ObservableCollection<CommandOperationDefinition>();
        ParameterInputs = new ObservableCollection<ParameterInputViewModel>();

        SendCommand = new AsyncRelayCommand(SendAsync);
        PauseCommand = new AsyncRelayCommand(() => ExecuteControlAsync(_client.PauseAsync, "Pause"));
        ResumeCommand = new AsyncRelayCommand(() => ExecuteControlAsync(_client.ResumeAsync, "Resume"));
        ClearCommand = new AsyncRelayCommand(() => ExecuteControlAsync(_client.ClearAsync, "Clear"));
        AbortCommand = new AsyncRelayCommand(() => ExecuteControlAsync(_client.AbortCurrentAsync, "Abort"));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, __) => await RefreshStatusAsync();

        _ = InitializeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeviceCommandDefinition> Devices { get; }

    public ObservableCollection<CommandOperationDefinition> Operations { get; }

    public ObservableCollection<ParameterInputViewModel> ParameterInputs { get; }

    public IAsyncRelayCommand SendCommand { get; }

    public IAsyncRelayCommand PauseCommand { get; }

    public IAsyncRelayCommand ResumeCommand { get; }

    public IAsyncRelayCommand ClearCommand { get; }

    public IAsyncRelayCommand AbortCommand { get; }

    public DeviceCommandDefinition? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!SetField(ref _selectedDevice, value))
            {
                return;
            }

            RebuildOperations();
            RebuildParameterInputs();
        }
    }

    public CommandOperationDefinition? SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (!SetField(ref _selectedOperation, value))
            {
                return;
            }

            RebuildParameterInputs();
        }
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
            if (SelectedDevice == null || SelectedOperation == null)
            {
                StatusText = "Please select a device and operation.";
                return;
            }

            var request = new DeviceCommandRequest
            {
                DeviceType = SelectedDevice.DeviceType,
                DriverId = SelectedDevice.DriverId,
                Operation = SelectedOperation.Name,
                Parameters = BuildParametersDictionary(),
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

    private async Task InitializeAsync()
    {
        await LoadCapabilitiesAsync();
        _timer.Start();
    }

    private async Task LoadCapabilitiesAsync()
    {
        try
        {
            var capabilities = await _client.GetCapabilitiesAsync();
            var data = (capabilities == null || capabilities.Count == 0)
                ? BuildFallbackCatalog().ToList()
                : capabilities;

            Devices.Clear();
            foreach (var capability in data)
            {
                Devices.Add(capability);
            }

            SelectedDevice = Devices.FirstOrDefault();
        }
        catch
        {
            var fallback = BuildFallbackCatalog();
            Devices.Clear();
            foreach (var item in fallback)
            {
                Devices.Add(item);
            }

            SelectedDevice = Devices.FirstOrDefault();
        }
    }

    private async Task ExecuteControlAsync(Func<Task> action, string actionName)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusText = $"{actionName} failed: {ex.Message}";
        }
    }

    private void RebuildOperations()
    {
        Operations.Clear();
        if (SelectedDevice == null)
        {
            SelectedOperation = null;
            return;
        }

        foreach (var operation in SelectedDevice.Operations)
        {
            Operations.Add(operation);
        }

        SelectedOperation = Operations.FirstOrDefault();
    }

    private void RebuildParameterInputs()
    {
        ParameterInputs.Clear();

        if (SelectedOperation == null)
        {
            OnPropertyChanged(nameof(ParameterInputs));
            return;
        }

        foreach (var parameter in SelectedOperation.Parameters)
        {
            ParameterInputs.Add(new ParameterInputViewModel(parameter));
        }

        OnPropertyChanged(nameof(ParameterInputs));
    }

    private Dictionary<string, object> BuildParametersDictionary()
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in ParameterInputs)
        {
            dict[input.Name] = ConvertParameterValue(input);
        }

        return dict;
    }

    private static object ConvertParameterValue(ParameterInputViewModel input)
    {
        var raw = input.ValueText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        return input.Type switch
        {
            ParameterValueType.Integer => int.Parse(raw, CultureInfo.InvariantCulture),
            ParameterValueType.Decimal => decimal.Parse(raw, CultureInfo.InvariantCulture),
            ParameterValueType.Boolean => bool.Parse(raw),
            _ => raw
        };
    }

    private static IReadOnlyList<DeviceCommandDefinition> BuildFallbackCatalog()
    {
        return new List<DeviceCommandDefinition>
        {
            new DeviceCommandDefinition
            {
                DeviceType = "DMM",
                DriverId = "default",
                Operations = new List<CommandOperationDefinition>
                {
                    new CommandOperationDefinition
                    {
                        Name = "MeasureVoltage",
                        Parameters = new List<CommandParameterDefinition>
                        {
                            new CommandParameterDefinition { Name = "range", Type = ParameterValueType.Decimal, DefaultValue = "10.0" },
                            new CommandParameterDefinition { Name = "channel", Type = ParameterValueType.Integer, DefaultValue = "1" }
                        }
                    },
                    new CommandOperationDefinition { Name = "Identify" }
                }
            },
            new DeviceCommandDefinition
            {
                DeviceType = "PSU",
                DriverId = "default",
                Operations = new List<CommandOperationDefinition>
                {
                    new CommandOperationDefinition
                    {
                        Name = "SetVoltage",
                        Parameters = new List<CommandParameterDefinition>
                        {
                            new CommandParameterDefinition { Name = "voltage", Type = ParameterValueType.Decimal, DefaultValue = "5.0" },
                            new CommandParameterDefinition { Name = "currentLimit", Type = ParameterValueType.Decimal, DefaultValue = "1.0" }
                        }
                    },
                    new CommandOperationDefinition { Name = "Identify" }
                }
            }
        };
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? memberName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(memberName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? memberName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
}

public sealed class ParameterInputViewModel : INotifyPropertyChanged
{
    private string _valueText;

    public ParameterInputViewModel(CommandParameterDefinition definition)
    {
        Name = definition.Name;
        Type = definition.Type;
        IsRequired = definition.IsRequired;
        _valueText = definition.DefaultValue ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }

    public ParameterValueType Type { get; }

    public bool IsRequired { get; }

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (_valueText == value)
            {
                return;
            }

            _valueText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueText)));
        }
    }
}
