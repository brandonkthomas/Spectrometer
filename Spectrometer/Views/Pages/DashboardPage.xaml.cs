using Spectrometer.Models;
using Spectrometer.Services;
using Spectrometer.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class DashboardPage : INavigableView<DashboardViewModel>
{
    public DashboardViewModel ViewModel { get; }

    private readonly ApplicationHostService? _hostService;

    public DashboardPage(DashboardViewModel viewModel, ApplicationHostService hostService)
    {
        Logger.Write("DashboardPage initializing...");
        ViewModel = viewModel;
        DataContext = viewModel;

        _hostService = hostService;

        InitializeComponent();
        Logger.Write("DashboardPage initialized");
    }

    // -------------------------------------------------------------------------------------------
    // Click Events
    // -------------------------------------------------------------------------------------------

    private void CpuCard_Click(object sender, RoutedEventArgs e)
    {
        _hostService?.Navigate(typeof(SensorsPage));
    }
}
