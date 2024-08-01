using Spectrometer.Services;
using Spectrometer.ViewModels.Windows;

namespace Spectrometer.ViewModels.Pages;

public partial class GraphsViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public bool IsLoading => _mainWindowViewModel.IsLoading;

    public HardwareMonitorService? HwMonSvc => _mainWindowViewModel?.HwMonSvc ?? throw new Exception("The Hardware Monitor Service could not be initialized.");

    public GraphsViewModel(MainWindowViewModel mainWindowViewModel) => _mainWindowViewModel = mainWindowViewModel;
}
