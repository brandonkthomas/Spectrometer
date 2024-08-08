using Spectrometer.Services;
using Spectrometer.ViewModels.Windows;
using System.ComponentModel;

namespace Spectrometer.ViewModels.Pages;

public partial class SensorsViewModel : ObservableObject
{
    // -------------------------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------------------------

    private readonly MainWindowViewModel _mainWindowViewModel;

    public bool IsLoading => _mainWindowViewModel.IsLoading;
    public string MemoryImagePath => _mainWindowViewModel.MemoryImagePath;
    public string StorageImagePath => _mainWindowViewModel.StorageImagePath;

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
    // Constructor
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Constructor -- wait for init before allowing properties to be accessed
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    public SensorsViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        WaitForInitialization();
    }

    // -------------------------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Update derived properties when the parent changes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Console.WriteLine($"PropertyChanged: {e.PropertyName}");

        if (e.PropertyName == nameof(MainWindowViewModel.IsLoading))
            OnPropertyChanged(nameof(IsLoading));

        if (e.PropertyName == nameof(MainWindowViewModel.MemoryImagePath))
            OnPropertyChanged(nameof(MemoryImagePath));

        if (e.PropertyName == nameof(MainWindowViewModel.StorageImagePath))
            OnPropertyChanged(nameof(StorageImagePath));
    }

    /// <summary>
    /// We don't want to access these properties until the MainWindowViewModel has finished initializing.
    /// </summary>
    private async void WaitForInitialization()
    {
        await _mainWindowViewModel.InitializationTask;
        OnPropertyChanged(nameof(HwMonSvc));
    }
}
