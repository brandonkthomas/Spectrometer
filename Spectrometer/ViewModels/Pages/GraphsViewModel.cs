using Spectrometer.Services;
using Spectrometer.ViewModels.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Pages;

public partial class GraphsViewModel : ObservableObject, INavigationAware
{
    // -------------------------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------------------------

    private readonly MainWindowViewModel _mainWindowViewModel;

    public bool IsLoading => _mainWindowViewModel.IsLoading;

    public HardwareMonitorService? HwMonSvc => _mainWindowViewModel?.HwMonSvc ?? throw new Exception("The Hardware Monitor Service could not be initialized.");

    // -------------------------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    public GraphsViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    // -------------------------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    public void OnNavigatedTo()
    {
        // TODO: refresh Graphs page contents on navigation change
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnNavigatedFrom() { }
}
