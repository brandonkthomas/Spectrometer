using Spectrometer.ViewModels.Pages;
using System.Reflection.Metadata;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    public SettingsViewModel ViewModel { get; }

    // ------------------------------------------------------------------------------------------------
    // Constructor

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    // ------------------------------------------------------------------------------------------------
    // Event Handlers

    private void StartupTab_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.StartingTab = this.StartupTab.SelectedValue.ToString() ?? "Dashboard"; // default to Dashboard if somehow null
        App.SettingsMgr.SaveSettings();
    }
}
