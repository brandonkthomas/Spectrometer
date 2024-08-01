using Spectrometer.Models;
using Spectrometer.Services;
using Spectrometer.ViewModels.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Spectrometer.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public bool IsLoading => _mainWindowViewModel.IsLoading;
    public string CpuImagePath => _mainWindowViewModel.CpuImagePath;
    public string GpuImagePath => _mainWindowViewModel.GpuImagePath;

    public ObservableCollection<ProcessInfo?> ProcessesList => _mainWindowViewModel.PrcssInfoList;

    public HardwareMonitorService? HwMonSvc
    {
        get
        {
            if (_mainWindowViewModel.HwMonSvc is null)
                return null;
            return _mainWindowViewModel.HwMonSvc;
        }
    }

    /// <summary>
    /// Constructor -- wait for init before allowing properties to be accessed
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    public DashboardViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        WaitForInitialization();
    }

    /// <summary>
    /// Update derived properties when the parent changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsLoading))
            OnPropertyChanged(nameof(IsLoading));

        if (e.PropertyName == nameof(MainWindowViewModel.CpuImagePath))
            OnPropertyChanged(nameof(CpuImagePath));

        if (e.PropertyName == nameof(MainWindowViewModel.GpuImagePath))
            OnPropertyChanged(nameof(GpuImagePath));
    }

    /// <summary>
    /// We don't want to access these properties until the MainWindowViewModel has finished initializing.
    /// </summary>
    private async void WaitForInitialization()
    {
        await _mainWindowViewModel.InitializationTask;
        OnPropertyChanged(nameof(HwMonSvc));
        OnPropertyChanged(nameof(ProcessesList));
    }
}
