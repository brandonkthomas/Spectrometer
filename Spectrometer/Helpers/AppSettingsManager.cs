using Spectrometer.Views.Pages;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Spectrometer.Helpers;

// -------------------------------------------------------------------------------------------
/// <summary>
/// 
/// </summary>
public class AppSettings
{
    public string StartingTab { get; set; } = string.Empty;
    public int PollingRate { get; set; }
    public bool StartWithWindows { get; set; }
}

// -------------------------------------------------------------------------------------------
/// <summary>
/// 
/// </summary>
public class AppSettingsManager
{
    private readonly string _settingsFilePath;

    public AppSettings? Settings { get; private set; }

    // -------------------------------------------------------------------------------------------
    // Constructor - get file and call LoadSettings()
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public AppSettingsManager()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "Spectrometer");
        Directory.CreateDirectory(directoryPath);

        _settingsFilePath = Path.Combine(directoryPath, Debugger.IsAttached ? "appSettings.Development.json" : "appSettings.json");

        LoadSettings();
    }

    // -------------------------------------------------------------------------------------------
    // Load & Save
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void LoadSettings()
    {
        // -------------------------------------------------------------------------------------------
        // Settings file exists, load it

        if (File.Exists(_settingsFilePath))
        {
            string json = File.ReadAllText(_settingsFilePath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        // -------------------------------------------------------------------------------------------
        // Settings file does not exist, create it w/ defaults

        else
        {
            Settings = new AppSettings() // defaults
            {
                StartingTab = "Dashboard",
                PollingRate = 1750,
                StartWithWindows = false
            };

            SaveSettings();
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public void SaveSettings()
    {
        string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    // -------------------------------------------------------------------------------------------
    // Specialized Retrieval Methods
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Type GetStartingTab()
    {
        string value;

        if (Settings is null)
            LoadSettings();

        if (Settings is null)
            return typeof(DashboardPage);
        else
            value = Settings.StartingTab;

        switch (value)
        {
            case "Dashboard":
                return typeof(DashboardPage);

            case "Graphs":
                return typeof(GraphsPage);

            case "Sensors":
                return typeof(GraphsPage);

            case "Settings":
                return typeof(SettingsPage);

            default:
                return typeof(DashboardPage);
        }
    }
}
