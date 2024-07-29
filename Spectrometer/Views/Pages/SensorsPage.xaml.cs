using Spectrometer.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages
{
    public partial class SensorsPage : INavigableView<SensorsViewModel>
    {
        public SensorsViewModel ViewModel { get; }

        public SensorsPage(SensorsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
