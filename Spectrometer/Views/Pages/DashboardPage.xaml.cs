using Spectrometer.Models;
using Spectrometer.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class DashboardPage : INavigableView<DashboardViewModel>
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
        Logger.Write("DashboardPage initialized");
    }
}
