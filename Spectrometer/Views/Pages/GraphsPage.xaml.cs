using Spectrometer.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class GraphsPage : INavigableView<GraphsViewModel>
{
    public GraphsViewModel ViewModel { get; }

    public GraphsPage(GraphsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
    }
}
