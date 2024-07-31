using Spectrometer.Models;
using Spectrometer.ViewModels.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Spectrometer.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public HardwareStatus? HwStatus => _mainWindowViewModel?.HwStatus ?? new();
    public ObservableCollection<ProcessInfo?> ProcessesList => _mainWindowViewModel.PrcssInfoList;

    public DashboardViewModel(MainWindowViewModel mainWindowViewModel) => _mainWindowViewModel = mainWindowViewModel;
}
