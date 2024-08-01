using Spectrometer.ViewModels.Pages;
using System.Reflection.Metadata;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    private void comboBox1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ViewModel.UpdateAppSettings(this.comboBox1.SelectedValue.ToString(), "StartingTab");
    }
}
