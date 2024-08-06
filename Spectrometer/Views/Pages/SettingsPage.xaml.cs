using Spectrometer.Models;
using Spectrometer.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    // ------------------------------------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------------------------------------

    public SettingsViewModel ViewModel { get; }

    // ------------------------------------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------------------------------------

    public SettingsPage(SettingsViewModel viewModel)
    {
        Logger.Write("SettingsPage initializing...");
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
        Logger.Write("SettingsPage initialized");
    }

    // ------------------------------------------------------------------------------------------------
    // Event Handlers
    // ------------------------------------------------------------------------------------------------

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

    // ------------------------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AutomaticallyCheckForUpdatesCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.AutomaticallyCheckForUpdates = true;
        App.SettingsMgr.SaveSettings();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AutomaticallyCheckForUpdatesCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.AutomaticallyCheckForUpdates = false;
        App.SettingsMgr.SaveSettings();
    }
}
