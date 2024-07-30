using Spectrometer.Models;
using Spectrometer.Services;
using System.Diagnostics;
using System.Timers;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Pages;

public partial class SensorsViewModel : ObservableObject, INavigationAware
{
    // ------------------------------------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------------------------------------

    [ObservableProperty]
    private HardwareMonitorService? _hwMonSvc;

    [ObservableProperty]
    private HardwareStatus? _hwStatus;

    private readonly System.Timers.Timer _timer;
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => PollSensors();

    private readonly int _defaultPollingInterval = 1750; // Default polling interval in milliseconds

    // ------------------------------------------------------------------------------------------------
    // Constructor + Events
    // ------------------------------------------------------------------------------------------------

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public SensorsViewModel()
    {
        HwStatus = new();
        HwStatus.IsLoading = true;

        _timer = new System.Timers.Timer(_defaultPollingInterval); // TODO: Make this a user setting
        _timer.Elapsed += OnTimerElapsed;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        if (HwStatus is not null)
            HwStatus.IsLoading = true;

        await Task.Run(() =>
        {
            HwMonSvc = HardwareMonitorService.Instance; // this takes a sec to finish

            GetCpuGpuImagePaths();
            PollSensors(); // run once before starting timer to get initial values
        });

        if (HwStatus is not null)
            HwStatus.IsLoading = false;

        _timer.Start();
    }

    // ------------------------------------------------------------------------------------------------
    // Navigation Detection
    // ------------------------------------------------------------------------------------------------

    public void OnNavigatedTo() => _timer.Stop();

    public void OnNavigatedFrom() => _timer.Start();

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void PollSensors()
    {
        if (HwMonSvc is null || HwStatus is null)
            return;

        try
        {
            HwMonSvc.Update();

            HwStatus.MbName = HwMonSvc.GetMotherboardName();
            HwStatus.MbSensors = HwMonSvc.GetMotherboardSensors();

            HwStatus.CpuName = HwMonSvc.GetCpuName();
            HwStatus.CpuSensors = HwMonSvc.GetCpuSensors();

            HwStatus.GpuName = HwMonSvc.GetGpuName();
            HwStatus.GpuSensors = HwMonSvc.GetGpuSensors();

            HwStatus.MemorySensors = HwMonSvc.GetMemorySensors();

            HwStatus.StorageSensors = HwMonSvc.GetStorageSensors();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error polling sensors: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void GetCpuGpuImagePaths()
    {
        if (HwStatus is null)
            return;

        var isDarkMode = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

        HwStatus.CpuImagePath = GetImagePath(HwStatus.CpuName, isDarkMode, "intel", "amd", "ryzen");
        HwStatus.GpuImagePath = GetImagePath(HwStatus.GpuName, isDarkMode, "nvidia", "geforce", "amd", "radeon", "intel");
    }

    private string GetImagePath(string name, bool isDarkMode, params string[] keywords)
    {
        string logoColor = isDarkMode ? "white" : "black";

        foreach (var keyword in keywords)
            if (name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                return $"pack://application:,,,/Assets/{keyword}-logo-{logoColor}.png";

        return string.Empty;
    }

    // ------------------------------------------------------------------------------------------------
    // Dispose
    // ------------------------------------------------------------------------------------------------

    public void Dispose()
    {
        _timer?.Dispose();
        HwMonSvc?.Dispose();
    }
}
