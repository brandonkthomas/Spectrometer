using Microsoft.Win32;
using Spectrometer.Models;
using System.Configuration;
using System.IO;
using System.Timers;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    // -------------------------------------------------------------------------------------------
    // Local Fields
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    // Calculated Local Fields (not saved to AppSettings)

    private bool _isInitialized = false;

    [ObservableProperty]
    private string _appVersion = String.Empty;

    [ObservableProperty]
    private List<string> _listOfPages = [];

    // -------------------------------------------------------------------------------------------
    // Saved Settings

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private int _pollingRate = 0;

    [ObservableProperty]
    private string _selectedStartTab = string.Empty;

    [ObservableProperty]
    private bool _startWithWindows = false;

    // -------------------------------------------------------------------------------------------
    // Saved Settings

    private System.Timers.Timer? _pollingRateChangeTimer;
    private Action? _pollingRateChangeAction;

    // -------------------------------------------------------------------------------------------
    // Constructor + Initialization
    // -------------------------------------------------------------------------------------------

    public SettingsViewModel() { }

    private void InitializeViewModel()
    {
        // -------------------------------------------------------------------------------------------
        // Calculate local cached values

        AppVersion = $"Spectrometer v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty} (July 31, 2024)";

        ListOfPages =
        [
            "Dashboard",
            "Graphs",
            "Sensors",
            "Settings"
        ];

        // -------------------------------------------------------------------------------------------
        // Retrieve AppSettings

        if (App.SettingsMgr is null || App.SettingsMgr.Settings is null)
            return; // todo... this should never happen

        var _config = App.SettingsMgr.Settings;

        // -------------------------------------------------------------------------------------------
        // Populate local values from AppSettings

        CurrentTheme = ApplicationThemeManager.GetAppTheme(); // TODO

        PollingRate = App.SettingsMgr.Settings.PollingRate;

        SelectedStartTab = _config.StartingTab ?? "Dashboard";

        try
        {
            StartWithWindows = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)?.GetValue("Spectrometer") != null;
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }

        // -------------------------------------------------------------------------------------------
        // Configure Polling Rate field auto-save timer
        // -------------------------------------------------------------------------------------------

        _pollingRateChangeTimer = new System.Timers.Timer(1000); // 1000ms => 1s
        _pollingRateChangeTimer.Elapsed += PollingRateChangeTimerElapsed;
        _pollingRateChangeAction = () => OnPollingRateChange(PollingRate);

        _isInitialized = true;
    }

    public void OnNavigatedTo()
    {
        if (!_isInitialized)
            InitializeViewModel();
    }

    public void OnNavigatedFrom() { }

    // -------------------------------------------------------------------------------------------
    // Theme change handler

    [RelayCommand]
    private void OnChangeTheme(string parameter)
    {
        switch (parameter)
        {
            case "theme_light":
                if (CurrentTheme == ApplicationTheme.Light)
                    break;

                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                CurrentTheme = ApplicationTheme.Light;

                break;

            default:
                if (CurrentTheme == ApplicationTheme.Dark)
                    break;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                CurrentTheme = ApplicationTheme.Dark;

                break;
        }
    }

    // -------------------------------------------------------------------------------------------
    // Startup with Windows handler

    [RelayCommand]
    private void OnChangeStartWithWindows(bool parameter)
    {
        try
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Spectrometer.exe") ?? string.Empty; // todo: set a default location

            switch (parameter)
            {
                case true:
                    rk?.SetValue("Spectrometer", exePath);
                    break;
                case false:
                    rk?.DeleteValue("Spectrometer", false);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Polling Rate event handler
    //   1 second after the user stops typing, update + save the polling rate automatically

    [RelayCommand]
    private void OnPollingRateChange(int parameter)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.PollingRate = parameter;
        App.SettingsMgr.SaveSettings();
    }

    private void PollingRateChangeTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _pollingRateChangeTimer?.Stop();
        App.Current.Dispatcher.Invoke(_pollingRateChangeAction);
    }

    public void StartPollingRateChangeTimer()
    {
        _pollingRateChangeTimer?.Stop();
        _pollingRateChangeTimer?.Start();
    }

    // ------------------------------------------------------------------------------------------------
    // Starting Tab event handler

    private void OnStartingTabChange(string parameter)
    {
        if (App.SettingsMgr?.Settings is null) return;
        App.SettingsMgr.Settings.StartingTab = parameter;
        App.SettingsMgr.SaveSettings();
    }

    // ------------------------------------------------------------------------------------------------
    // Starting Tab event handler

    public void UpdateAppSettings(string parameter, string settingName)
    {
        var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var settings = configFile.AppSettings.Settings;
        settings[settingName].Value = parameter;

        configFile.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
    }
}
