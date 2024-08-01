﻿using Microsoft.Win32;
using Spectrometer.Models;
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
            string exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty; // todo: set a default location

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
}
