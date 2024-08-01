using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    // -------------------------------------------------------------------------------------------
    // Fields

    private bool _isInitialized = false;

    [ObservableProperty]
    private string _appVersion = String.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private bool _startWithWindows = false;

    // -------------------------------------------------------------------------------------------
    // Init

    public SettingsViewModel() { }

    private void InitializeViewModel()
    {
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"Spectrometer v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty} (July 31, 2024)";
        StartWithWindows = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true).GetValue("Spectrometer") != null ? true : false;

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
        RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        switch (parameter)
        {
            case true:
                rk.SetValue("Spectrometer", System.Reflection.Assembly.GetEntryAssembly().Location);
                break;
            case false:
                rk.DeleteValue("Spectrometer", false);
                break;
        }
    }
}
