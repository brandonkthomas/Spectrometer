using Spectrometer.Models;
using Spectrometer.ViewModels.Windows;

namespace Spectrometer.ViewModels.Pages;

public partial class GraphsViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public HardwareStatus? HwStatus => _mainWindowViewModel?.HwStatus ?? new();

    public GraphsViewModel(MainWindowViewModel mainWindowViewModel) => _mainWindowViewModel = mainWindowViewModel;
}
