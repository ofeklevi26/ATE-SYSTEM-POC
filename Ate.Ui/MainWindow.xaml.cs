using System.Windows;
using Ate.Ui.ViewModels;

namespace Ate.Ui;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
