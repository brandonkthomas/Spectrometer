using Spectrometer.Models;
using Spectrometer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows.Data;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    // ------------------------------------------------------------------------------------------------
    // Application-critical Properties
    // ------------------------------------------------------------------------------------------------

    [ObservableProperty]
    private string _applicationTitle = "Spectrometer";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
        new NavigationViewItem()
        {
            Content = "Dashboard",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
            TargetPageType = typeof(Views.Pages.DashboardPage)
        },
        new NavigationViewItem()
        {
            Content = "Graphs",
            Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowTrending24 },
            TargetPageType = typeof(Views.Pages.GraphsPage)
        },
        new NavigationViewItem()
        {
            Content = "Sensors",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Book24 },
            TargetPageType = typeof(Views.Pages.SensorsPage)
        }
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems = 
    [
        new NavigationViewItem()
        {
            Content = "Settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            TargetPageType = typeof(Views.Pages.SettingsPage)
        }
    ];

    [ObservableProperty]
    private ObservableCollection<MenuItem> _trayMenuItems = 
    [
        new MenuItem { Header = "Home", Tag = "tray_home" }
    ];

    // ------------------------------------------------------------------------------------------------
    // HW Monitor Service + Sensor Collection
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

    /// <summary>
    /// Sensor poll timer
    /// </summary>
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

    public MainWindowViewModel()
    {
        HwStatus = new();
        HwStatus.IsLoading = true;

        PrcssInfoList = new();
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
        Logger.Write("DashboardViewModel initializing...");

        if (HwStatus is not null) HwStatus.IsLoading = true;

        await Task.Run(() =>
        {
            // This takes a sec to initialize; await it in a new thread so we don't block the UI
            HwMonSvc = HardwareMonitorService.Instance;
            PrcssSvc = ProcessesService.Instance;

            GetCpuGpuImagePaths();

            // Run these once before starting timer to get initial values
            Stopwatch stopwatch = new();
            stopwatch.Start();

            PollSensors();

            stopwatch.Stop();
            Logger.Write($"Sensors polled in {stopwatch.ElapsedMilliseconds}ms");

            GetProcesses();
            Logger.Write($"{PrcssInfoList.Count} processes found");
        });

        if (HwStatus is not null) HwStatus.IsLoading = false;

        _timer.Start();

        Logger.Write("DashboardViewModel initialized");
    }

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

            HwStatus.MbSensors = HwMonSvc.GetMotherboardSensors();
            HwStatus.MbName = HwMonSvc.GetMotherboardName();

            HwStatus.CpuSensors = HwMonSvc.GetCpuSensors();
            HwStatus.CpuName = HwMonSvc.GetCpuName();
            HwStatus.CpuTemp = HwMonSvc.GetCpuTemp();
            HwStatus.CpuUsage = HwMonSvc.GetCpuUsage();
            HwStatus.CpuPower = HwMonSvc.GetCpuPowerCurrent();
            HwStatus.CpuPowerMax = HwMonSvc.GetCpuPowerMax();

            HwStatus.GpuSensors = HwMonSvc.GetGpuSensors();
            HwStatus.GpuName = HwMonSvc.GetGpuName();
            HwStatus.GpuTemp = HwMonSvc.GetGpuTemp();
            HwStatus.GpuUsage = HwMonSvc.GetGpuUsage();
            HwStatus.GpuMemoryTotal = HwMonSvc.GetGpuMemoryTotal();
            HwStatus.GpuMemoryUsage = HwMonSvc.GetGpuMemoryUsage();
            HwStatus.GpuPower = HwMonSvc.GetGpuPowerCurrent();
            HwStatus.GpuPowerMax = HwMonSvc.GetGpuPowerMax();

            HwStatus.MemorySensors = HwMonSvc.GetMemorySensors();
            HwStatus.MemoryUsageGb = HwMonSvc.GetMemoryUsageGb();
            HwStatus.MemoryTotalGb = HwMonSvc.GetMemoryTotalGb();
            float usedMemoryPercent = HwMonSvc.GetMemoryUsagePercent();
            HwStatus.MemoryUsageDetails = $"{usedMemoryPercent:F0}% ({HwStatus.MemoryUsageGb:F1} GB / {HwStatus.MemoryTotalGb:F1} GB)";

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
    private void GetProcesses()
    {
        if (PrcssSvc is null)
            return;

        var processes = PrcssSvc.LoadProcesses();

        try
        {
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
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
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

        var isDarkMode = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

        HwStatus.CpuImagePath = GetImagePath(HwStatus.CpuName, isDarkMode, "intel", "amd", "qualcomm");
        HwStatus.GpuImagePath = GetImagePath(HwStatus.GpuName, isDarkMode, "nvidia", "amd", "intel");
    }

    private string GetImagePath(string name, bool isDarkMode, params string[] keywords)
    {
        string logoColor = isDarkMode ? "white" : "black";

        foreach (var keyword in keywords)
            if (name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                return $"pack://application:,,,/Assets/{keyword}-logo-{logoColor}.png";

        return string.Empty;
    }
}
