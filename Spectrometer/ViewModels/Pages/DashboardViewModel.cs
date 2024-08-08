using Spectrometer.Models;
using Spectrometer.Services;
using Spectrometer.ViewModels.Windows;
using System.ComponentModel;

namespace Spectrometer.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject
{
    // -------------------------------------------------------------------------------------------
    // Fields

    private readonly MainWindowViewModel _mainWindowViewModel;

    public bool IsLoading => _mainWindowViewModel.IsLoading;

    public HardwareMonitorService? HwMonSvc
    {
        get
        {
            if (_mainWindowViewModel.HwMonSvc is null)
                return null;
            return _mainWindowViewModel.HwMonSvc;
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Constructor -- wait for init before allowing properties to be accessed
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    public DashboardViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        WaitForMainWindowInitialization();
        Logger.Write("DashboardViewModel initialized");
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
    }

    /// <summary>
    /// We don't want to access these properties until the MainWindowViewModel has finished initializing.
    /// </summary>
    private async void WaitForMainWindowInitialization()
    {
        await _mainWindowViewModel.InitializationTask;
        OnPropertyChanged(nameof(HwMonSvc));
    }
}
