using System;
using System.Windows;
using Ate.Ui.ViewModels;

namespace Ate.Ui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new MainViewModel();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private async void OnSendClick(object sender, RoutedEventArgs e)
    {
        await _vm.SendAsync();
    }

    private async void OnPauseClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.PauseAsync();
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"Pause failed: {ex.Message}";
        }
    }

    private async void OnResumeClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.ResumeAsync();
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"Resume failed: {ex.Message}";
        }
    }

    private async void OnClearClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.ClearAsync();
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"Clear failed: {ex.Message}";
        }
    }

    private async void OnAbortClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.AbortAsync();
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"Abort failed: {ex.Message}";
        }
    }
}
