using Spectrometer.Models;
using Spectrometer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows.Data;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject, INavigationAware
{
    // ------------------------------------------------------------------------------------------------
    // Private Fields
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Hardware monitor service
    /// </summary>
    [ObservableProperty]
    private HardwareMonitorService? _hwMonSvc;

    /// <summary>
    /// Hardware status collection
    /// </summary>
    [ObservableProperty]
    private HardwareStatus? _hwStatus;

    /// <summary>
    /// Process service
    /// </summary>
    [ObservableProperty]
    private ProcessesService? _prcssSvc;

    /// <summary>
    /// Process info collection
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProcessInfo?> _prcssInfoList;

    private CollectionViewSource _processesViewSource;
    public ICollectionView ProcessesView => _processesViewSource.View;

    private readonly System.Timers.Timer _timer;
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        PollSensors();
        GetProcesses();
    }

    private readonly int _defaultPollingInterval = 1750; // Default polling interval in milliseconds

    // ------------------------------------------------------------------------------------------------
    // Constructor + Events
    // ------------------------------------------------------------------------------------------------

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public DashboardViewModel()
    {
        HwStatus = new();
        PrcssInfoList = new();
        HwStatus.IsLoading = true;
        _processesViewSource = new CollectionViewSource { Source = PrcssInfoList };
        _processesViewSource.Filter += TopThreeFilter;

        _timer = new System.Timers.Timer(_defaultPollingInterval); // TODO: Make this a user setting
        _timer.Elapsed += OnTimerElapsed;

        InitializeAsync();
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private async void InitializeAsync()
    {
        if (HwStatus is not null) HwStatus.IsLoading = true;

        await Task.Run(() =>
        {
            // This takes a sec to initialize; await it in a new thread so we don't block the UI
            HwMonSvc = HardwareMonitorService.Instance;
            PrcssSvc = ProcessesService.Instance;

            GetCpuGpuImagePaths();

            // Run these once before starting timer to get initial values
            PollSensors(); 
            GetProcesses();
        });

        if (HwStatus is not null) HwStatus.IsLoading = false;

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

            HwStatus.CpuName = HwMonSvc.GetCpuName();
            HwStatus.CpuTemp = HwMonSvc.GetCpuTemp();
            HwStatus.CpuUsage = HwMonSvc.GetCpuUsage();
            HwStatus.CpuPower = HwMonSvc.GetCpuPowerCurrent();
            HwStatus.CpuPowerMax = HwMonSvc.GetCpuPowerMax();

            HwStatus.GpuName = HwMonSvc.GetGpuName();
            HwStatus.GpuTemp = HwMonSvc.GetGpuTemp();
            HwStatus.GpuUsage = HwMonSvc.GetGpuUsage();
            HwStatus.GpuPower = HwMonSvc.GetGpuPowerCurrent();
            //HwStatus.GpuPowerMax = HwMonSvc.GetGpuPowerMax();

            HwStatus.MemoryUsageGb = HwMonSvc.GetMemoryUsageGb();
            HwStatus.MemoryTotalGb = HwMonSvc.GetMemoryTotalGb();
            float usedMemoryPercent = HwMonSvc.GetMemoryUsagePercent();
            HwStatus.MemoryUsageDetails = $"{usedMemoryPercent:F0}% ({HwStatus.MemoryUsageGb:F1} GB / {HwStatus.MemoryTotalGb:F1} GB)";
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
    private void GetProcesses()
    {
        if (PrcssSvc is null)
            return;
        
        var processes = PrcssSvc.LoadProcesses();

        Application.Current.Dispatcher.Invoke(() =>
        {
            PrcssInfoList.Clear();
            foreach (var proc in processes)
            {
                PrcssInfoList.Add(proc);
            }

            ProcessesView.Refresh();
        });
    }

    private void TopThreeFilter(object sender, FilterEventArgs e)
    {
        var view = (CollectionView)_processesViewSource.View;
        var sortedProcesses = view.Cast<ProcessInfo>().ToList();
        e.Accepted = sortedProcesses.IndexOf((ProcessInfo)e.Item) < 3;
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void GetCpuGpuImagePaths()
    {
        if (HwStatus is null)
            return;

        if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark) // dark mode -- use white logo
        {
            // CPU
            if (HwStatus.CpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.CpuImagePath = "pack://application:,,,/Assets/intel-logo-white.png";
            else if (HwStatus.CpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || HwStatus.CpuName.Contains("ryzen", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.CpuImagePath = "pack://application:,,,/Assets/amd-logo-white.png";

            // GPU
            if (HwStatus.GpuName.Contains("nvidia", StringComparison.CurrentCultureIgnoreCase) || HwStatus.GpuName.Contains("geforce", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.GpuImagePath = "pack://application:,,,/Assets/nvidia-logo-white.png";
            else if (HwStatus.GpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || HwStatus.GpuName.Contains("radeon", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.GpuImagePath = "pack://application:,,,/Assets/amd-logo-white.png";
            else if (HwStatus.GpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.GpuImagePath = "pack://application:,,,/Assets/intel-logo-white.png";
        }
        else // dark mode -- use black logo
        {
            // CPU
            if (HwStatus.CpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.CpuImagePath = "pack://application:,,,/Assets/intel-logo-black.png";
            else if (HwStatus.CpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || HwStatus.CpuName.Contains("ryzen", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.CpuImagePath = "pack://application:,,,/Assets/amd-logo-black.png";

            // GPU
            if (HwStatus.GpuName.Contains("nvidia", StringComparison.CurrentCultureIgnoreCase) || HwStatus.GpuName.Contains("geforce", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.GpuImagePath = "pack://application:,,,/Assets/nvidia-logo-black.png";
            else if (HwStatus.GpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || HwStatus.GpuName.Contains("radeon", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.GpuImagePath = "pack://application:,,,/Assets/amd-logo-black.png";
            else if (HwStatus.GpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                HwStatus.GpuImagePath = "pack://application:,,,/Assets/intel-logo-black.png";
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Dispose
    // ------------------------------------------------------------------------------------------------

    public void Dispose()
    {
        _timer?.Dispose();
        HwMonSvc?.Dispose();

        PrcssInfoList.Clear();
        PrcssSvc?.Dispose();
    }
}
