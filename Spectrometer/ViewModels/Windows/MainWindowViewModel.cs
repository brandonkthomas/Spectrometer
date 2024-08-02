using Spectrometer.Models;
using Spectrometer.Services;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Timers;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Spectrometer.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    // -------------------------------------------------------------------------------------------
    // Application-critical Properties
    // -------------------------------------------------------------------------------------------

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

    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>
    /// Tabs need to wait for initialization before they can access the HW monitor service
    /// </summary>
    private readonly TaskCompletionSource<bool> _initializationTcs = new TaskCompletionSource<bool>();

    // -------------------------------------------------------------------------------------------
    // HW Monitor Service + Sensor Collection
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Hardware monitor service
    /// </summary>
    [ObservableProperty]
    private HardwareMonitorService? _hwMonSvc;

    /// <summary>
    /// Sensor poll timer
    /// </summary>
    private readonly System.Timers.Timer _timer;
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _timer.Interval = App.SettingsMgr?.Settings?.PollingRate ?? 1750; // reconfigure timer interval from AppSettings

        HwMonSvc?.Update();
        HwMonSvc?.PollAllSensors();
        HwMonSvc?.PollSpecificSensors();
        GetProcesses();
    }

    [ObservableProperty]
    private string _cpuImagePath = string.Empty;

    [ObservableProperty]
    private string _gpuImagePath = string.Empty;

    [ObservableProperty]
    private string _mbImagePath = string.Empty;

    [ObservableProperty]
    private string _memoryImagePath = string.Empty;

    [ObservableProperty]
    private string _storageImagePath = string.Empty;

    // -------------------------------------------------------------------------------------------
    // Process Service
    // -------------------------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------------------------
    // Constructor + Events
    // -------------------------------------------------------------------------------------------

    public MainWindowViewModel()
    {
        IsLoading = true;

        PrcssInfoList = [];

        _timer = new System.Timers.Timer(App.SettingsMgr?.Settings?.PollingRate ?? 1750); // 1750 = default
        _timer.Elapsed += OnTimerElapsed;

        InitializeAsync();
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private async void InitializeAsync()
    {
        Logger.Write("MainWindowViewModel initializing...");

        IsLoading = true;

        await Task.Run(() =>
        {
            // This takes a sec to initialize; await it in a new thread so we don't block the UI
            // Will also poll all sensors initially in the ctor

            HwMonSvc = HardwareMonitorService.Instance;

            PrcssSvc = ProcessesService.Instance;

            GetManufacturerImagePaths();

            GetProcesses();
            Logger.Write($"{PrcssInfoList.Count} processes found");
        });

        IsLoading = false;

        _timer.Start();
        _initializationTcs.SetResult(true); // Signal that initialization is complete

        Logger.Write("MainWindowViewModel initialized");
    }

    public Task InitializationTask => _initializationTcs.Task;

    // -------------------------------------------------------------------------------------------
    // Process Service helpers
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
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
            });
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }

    // -------------------------------------------------------------------------------------------
    // Image path calculations
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void GetManufacturerImagePaths()
    {
        if (HwMonSvc is null)
            return; // todo: placeholder image (maybe? should we just leave invisible?)

        var isDarkMode = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

        CpuImagePath = GetImagePath(HwMonSvc.CpuName, isDarkMode, "intel", "amd", "qualcomm");
        GpuImagePath = GetImagePath(HwMonSvc.GpuName, isDarkMode, "nvidia", "amd", "intel");
        MbImagePath = GetImagePath(HwMonSvc.MbName, isDarkMode, "msi", "asus");
    }

    private string GetImagePath(string name, bool isDarkMode, params string[] keywords)
    {
        string logoColor = isDarkMode ? "white" : "black";

        foreach (var keyword in keywords)
            if (name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                return $"pack://application:,,,/Assets/Manufacturers/{keyword}-logo-{logoColor}.png";

        return string.Empty;
    }
}
