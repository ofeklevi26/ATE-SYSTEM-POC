using System.Windows;
using Ate.Ui.ViewModels;

namespace Ate.Ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
