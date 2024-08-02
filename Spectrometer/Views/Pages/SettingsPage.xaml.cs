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
    // ------------------------------------------------------------------------------------------------

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    // ------------------------------------------------------------------------------------------------
    // Event Handlers
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void StartupTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.StartingTab = this.StartupTab.SelectedValue.ToString() ?? "Dashboard"; // default to Dashboard if somehow null
        App.SettingsMgr.SaveSettings();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SensorPollingRateSave_Click(object sender, RoutedEventArgs e)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.PollingRate = (int?)this.SensorPollingRateNumberBox.Value ?? 1750;
        App.SettingsMgr.SaveSettings();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameter"></param>
    private void OnStartingTabChange(string parameter)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.StartingTab = parameter;
        App.SettingsMgr.SaveSettings();
    }


}
