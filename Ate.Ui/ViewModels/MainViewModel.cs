using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Ate.Contracts;
using GalaSoft.MvvmLight.Command;
using Ate.Ui.Services;

namespace Ate.Ui.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AteClient _client = new AteClient();
    private readonly DispatcherTimer _timer;

    private DeviceDefinition _selectedDevice;
    private OperationDefinition _selectedOperation;
    private string _statusText = "Disconnected";

    public MainViewModel()
    {
        Devices = new ObservableCollection<DeviceDefinition>(BuildDeviceCatalog());
        _selectedDevice = Devices.First();
        Operations = new ObservableCollection<OperationDefinition>(_selectedDevice.Operations);
        _selectedOperation = Operations.First();
        ParameterInputs = new ObservableCollection<ParameterInputViewModel>();
        RebuildParameterInputs();

        SendCommand = new RelayCommand(async () => await SendAsync());
        PauseCommand = new RelayCommand(async () => await ExecuteControlAsync(_client.PauseAsync, "Pause"));
        ResumeCommand = new RelayCommand(async () => await ExecuteControlAsync(_client.ResumeAsync, "Resume"));
        ClearCommand = new RelayCommand(async () => await ExecuteControlAsync(_client.ClearAsync, "Clear"));
        AbortCommand = new RelayCommand(async () => await ExecuteControlAsync(_client.AbortCurrentAsync, "Abort"));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, __) => await RefreshStatusAsync();
        _timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeviceDefinition> Devices { get; }

    public ObservableCollection<OperationDefinition> Operations { get; }

    public ObservableCollection<ParameterInputViewModel> ParameterInputs { get; }

    public ICommand SendCommand { get; }

    public ICommand PauseCommand { get; }

    public ICommand ResumeCommand { get; }

    public ICommand ClearCommand { get; }

    public ICommand AbortCommand { get; }

    public DeviceDefinition SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (value == null)
            {
                return;
            }

            if (SetField(ref _selectedDevice, value))
            {
                RebuildOperations();
                RebuildParameterInputs();
            }
        }
    }

    public OperationDefinition SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (value == null)
            {
                return;
            }

            if (SetField(ref _selectedOperation, value))
            {
                RebuildParameterInputs();
            }
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
        foreach (var op in SelectedDevice.Operations)
        {
            Operations.Add(op);
        }

        var firstOperation = Operations.FirstOrDefault();
        if (firstOperation != null)
        {
            SelectedOperation = firstOperation;
            return;
        }

        ParameterInputs.Clear();
        OnPropertyChanged(nameof(ParameterInputs));
    }

    private void RebuildParameterInputs()
    {
        ParameterInputs.Clear();

        var operation = SelectedOperation;
        if (operation == null)
        {
            OnPropertyChanged(nameof(ParameterInputs));
            return;
        }

        foreach (var parameter in operation.Parameters)
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
            dict[input.Key] = ConvertParameterValue(input);
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
            ParameterType.Integer => int.Parse(raw, CultureInfo.InvariantCulture),
            ParameterType.Decimal => decimal.Parse(raw, CultureInfo.InvariantCulture),
            ParameterType.Boolean => bool.Parse(raw),
            _ => raw
        };
    }

    private static IReadOnlyList<DeviceDefinition> BuildDeviceCatalog()
    {
        return new[]
        {
            new DeviceDefinition(
                "DMM",
                new[]
                {
                    new OperationDefinition("MeasureVoltage", new[]
                    {
                        new ParameterDefinition("range", ParameterType.Decimal, "10.0"),
                        new ParameterDefinition("channel", ParameterType.Integer, "1")
                    }),
                    new OperationDefinition("Identify", Array.Empty<ParameterDefinition>())
                }),
            new DeviceDefinition(
                "PSU",
                new[]
                {
                    new OperationDefinition("SetVoltage", new[]
                    {
                        new ParameterDefinition("voltage", ParameterType.Decimal, "5.0"),
                        new ParameterDefinition("currentLimit", ParameterType.Decimal, "1.0")
                    }),
                    new OperationDefinition("SetCurrentLimit", new[]
                    {
                        new ParameterDefinition("currentLimit", ParameterType.Decimal, "1.0")
                    }),
                    new OperationDefinition("SetOutput", new[]
                    {
                        new ParameterDefinition("enabled", ParameterType.Boolean, "true")
                    }),
                    new OperationDefinition("OutputOn", new[]
                    {
                        new ParameterDefinition("state", ParameterType.Boolean, "true")
                    }),
                    new OperationDefinition("OutputOff", System.Array.Empty<ParameterDefinition>()),
                    new OperationDefinition("Identify", System.Array.Empty<ParameterDefinition>())
                })
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

public sealed class DeviceDefinition
{
    public DeviceDefinition(string deviceType, IReadOnlyList<OperationDefinition> operations)
    {
        DeviceType = deviceType;
        Operations = operations;
    }

    public string DeviceType { get; }

    public IReadOnlyList<OperationDefinition> Operations { get; }

    public override string ToString() => DeviceType;
}

public sealed class OperationDefinition
{
    public OperationDefinition(string name, IReadOnlyList<ParameterDefinition> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    public string Name { get; }

    public IReadOnlyList<ParameterDefinition> Parameters { get; }

    public override string ToString() => Name;
}

public sealed class ParameterDefinition
{
    public ParameterDefinition(string key, ParameterType type, string defaultValue)
    {
        Key = key;
        Type = type;
        DefaultValue = defaultValue;
    }

    public string Key { get; }

    public ParameterType Type { get; }

    public string DefaultValue { get; }
}

public sealed class ParameterInputViewModel : INotifyPropertyChanged
{
    private string _valueText;

    public ParameterInputViewModel(ParameterDefinition definition)
    {
        Key = definition.Key;
        Type = definition.Type;
        _valueText = definition.DefaultValue;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key { get; }

    public ParameterType Type { get; }

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

public enum ParameterType
{
    String,
    Integer,
    Decimal,
    Boolean
}
