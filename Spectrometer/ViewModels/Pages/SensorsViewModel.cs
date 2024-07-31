using Spectrometer.Models;
using Spectrometer.ViewModels.Windows;

namespace Spectrometer.ViewModels.Pages;

public partial class SensorsViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public HardwareStatus? HwStatus => _mainWindowViewModel?.HwStatus ?? new();

    public SensorsViewModel(MainWindowViewModel mainWindowViewModel) => _mainWindowViewModel = mainWindowViewModel;
}
