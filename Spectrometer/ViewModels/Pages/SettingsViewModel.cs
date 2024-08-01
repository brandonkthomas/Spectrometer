using Microsoft.Win32;
using Spectrometer.Models;
using System.Configuration;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.IO;
using Spectrometer.Helpers;

namespace Spectrometer.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    // -------------------------------------------------------------------------------------------
    // Fields

    private bool _isInitialized = false;

    private KeyValueConfigurationCollection _configuration;

    [ObservableProperty]
    private string _appVersion = String.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private bool _startWithWindows = false;

    [ObservableProperty]
    private int _pollingRate = 0;

    [ObservableProperty]
    private List<string> listOfPages = new List<string>();

    [ObservableProperty]
    private string selectedStartTab = string.Empty;

    // -------------------------------------------------------------------------------------------
    // Init

    public SettingsViewModel() { }

    private void InitializeViewModel()
    {
        var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        _configuration = configFile.AppSettings.Settings;
        var startTabHelper = new StartingTabPageHelper();
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"Spectrometer v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty} (July 31, 2024)";
        PollingRate = int.Parse(_configuration["PollRate"].Value.ToString());
        ListOfPages = startTabHelper.GetAllPageNames();
        SelectedStartTab = _configuration["StartingTab"].Value ?? "Dashboard";

        try
        {
            StartWithWindows = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)?.GetValue("Spectrometer") != null;
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }

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

    [RelayCommand]
    private void OnPollingRateChange(int parameter)
    {
        UpdateAppSettings(parameter.ToString(), "PollRate");
    }

    // ------------------------------------------------------------------------------------------------
    // Starting Tab event handler

    private void OnStartingTabChange(string parameter)
    {
        UpdateAppSettings(parameter, "StartingTab");
    }

    public void UpdateAppSettings(string parameter, string settingName)
    {
        var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var settings = configFile.AppSettings.Settings;
        settings[settingName].Value = parameter;

        configFile.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
    }
}
