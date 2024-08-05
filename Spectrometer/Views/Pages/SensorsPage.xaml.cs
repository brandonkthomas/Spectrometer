using Spectrometer.Models;
using Spectrometer.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class SensorsPage : INavigableView<SensorsViewModel>
{
    // -------------------------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------------------------

    public SensorsViewModel ViewModel { get; }

    // -------------------------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------------------------

    public SensorsPage(SensorsViewModel viewModel)
    {
        Logger.Write("SensorsPage initializing...");
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
        Logger.Write("SensorsPage initialized");
    }
}
