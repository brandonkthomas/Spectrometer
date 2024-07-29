using Spectrometer.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using Wpf.Ui.Appearance;

namespace Spectrometer.ViewModels.Pages;

public partial class DashboardViewModel : INotifyPropertyChanged
{
    // ------------------------------------------------------------------------------------------------
    // Public Fields
    // ------------------------------------------------------------------------------------------------
    #region Public Fields

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Motherboard

    private string _mbName = string.Empty;
    public string MbName
    {
        get => _mbName;
        set
        {
            _mbName = value;
            OnPropertyChanged();
        }
    }

    // ------------------------------------------------------------------------------------------------
    // CPU

    private string _cpuImagePath = string.Empty;
    public string CpuImagePath
    {
        get => _cpuImagePath;
        set
        {
            _cpuImagePath = value;
            OnPropertyChanged();
        }
    }

    private string _cpuName = string.Empty;
    public string CpuName
    {
        get => _cpuName;
        set
        {
            _cpuName = value;
            OnPropertyChanged();
        }
    }

    private float _cpuTemp;
    public float CpuTemp
    {
        get => _cpuTemp;
        set
        {
            _cpuTemp = value;
            OnPropertyChanged();
        }
    }

    private float _cpuUsage;
    public float CpuUsage
    {
        get => _cpuUsage;
        set
        {
            _cpuUsage = value;
            OnPropertyChanged();
        }
    }

    private float _cpuPower;
    public float CpuPower
    {
        get => _cpuPower;
        set
        {
            _cpuPower = value;
            OnPropertyChanged();
        }
    }

    private float _cpuPowerMax;
    public float CpuPowerMax
    {
        get => _cpuPowerMax;
        set
        {
            _cpuPowerMax = value;
            OnPropertyChanged();
        }
    }

    // ------------------------------------------------------------------------------------------------
    // GPU

    private string _gpuImagePath = string.Empty;
    public string GpuImagePath
    {
        get => _gpuImagePath;
        set
        {
            _gpuImagePath = value;
            OnPropertyChanged();
        }
    }

    private string _gpuName = string.Empty;
    public string GpuName
    {
        get => _gpuName;
        set
        {
            _gpuName = value;
            OnPropertyChanged();
        }
    }

    private float _gpuTemp;
    public float GpuTemp
    {
        get => _gpuTemp;
        set
        {
            _gpuTemp = value;
            OnPropertyChanged();
        }
    }

    private float _gpuUsage;
    public float GpuUsage
    {
        get => _gpuUsage;
        set
        {
            _gpuUsage = value;
            OnPropertyChanged();
        }
    }

    private float _gpuPower;
    public float GpuPower
    {
        get => _gpuPower;
        set
        {
            _gpuPower = value;
            OnPropertyChanged();
        }
    }

    private float _gpuPowerMax;
    public float GpuPowerMax
    {
        get => _gpuPowerMax;
        set
        {
            _gpuPowerMax = value;
            OnPropertyChanged();
            Debug.WriteLine($"GpuPowerMax: {value}");
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Memory

    private string _memoryUsageDetails = string.Empty;
    public string MemoryUsageDetails
    {
        get => _memoryUsageDetails;
        set
        {
            _memoryUsageDetails = value;
            OnPropertyChanged();
        }
    }

    private double _memoryUsageGb;
    public double MemoryUsageGb
    {
        get => _memoryUsageGb;
        set
        {
            _memoryUsageGb = value;
            OnPropertyChanged();
        }
    }

    private double _memoryTotalGb;
    public double MemoryTotalGb
    {
        get => _memoryTotalGb;
        set
        {
            _memoryTotalGb = value;
            OnPropertyChanged();
        }
    }
    #endregion

    // ------------------------------------------------------------------------------------------------
    // Private Fields
    // ------------------------------------------------------------------------------------------------

    private HardwareMonitorService? _hardwareMonitorService;
    private readonly System.Timers.Timer _timer;

    // ------------------------------------------------------------------------------------------------
    // Constructor + Events
    // ------------------------------------------------------------------------------------------------

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public DashboardViewModel()
    {
        _timer = new System.Timers.Timer(2000);
        _timer.Elapsed += OnTimerElapsed;
        InitializeAsync();
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private async void InitializeAsync()
    {
        IsLoading = true;
        await Task.Run(() => _hardwareMonitorService = new HardwareMonitorService());

        PollCycle(); // run once before starting timer to get initial values
        IsLoading = false;

        _timer.Start();

        //
        // Calculate CPU + GPU manufacturer logo paths
        //
        if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark) // dark mode -- use white logo
        {
            // CPU
            if (CpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                CpuImagePath = "pack://application:,,,/Assets/intel-logo-white.png";
            else if (CpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || CpuName.Contains("ryzen", StringComparison.CurrentCultureIgnoreCase))
                CpuImagePath = "pack://application:,,,/Assets/amd-logo-white.png";

            // GPU
            if (GpuName.Contains("nvidia", StringComparison.CurrentCultureIgnoreCase) || GpuName.Contains("geforce", StringComparison.CurrentCultureIgnoreCase))
                GpuImagePath = "pack://application:,,,/Assets/nvidia-logo-white.png";
            else if (GpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || GpuName.Contains("radeon", StringComparison.CurrentCultureIgnoreCase))
                GpuImagePath = "pack://application:,,,/Assets/amd-logo-white.png";
            else if (GpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                GpuImagePath = "pack://application:,,,/Assets/intel-logo-white.png";
        }
        else // dark mode -- use black logo
        {
            // CPU
            if (CpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                CpuImagePath = "pack://application:,,,/Assets/intel-logo-black.png";
            else if (CpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || CpuName.Contains("ryzen", StringComparison.CurrentCultureIgnoreCase))
                CpuImagePath = "pack://application:,,,/Assets/amd-logo-black.png";

            // GPU
            if (GpuName.Contains("nvidia", StringComparison.CurrentCultureIgnoreCase) || GpuName.Contains("geforce", StringComparison.CurrentCultureIgnoreCase))
                GpuImagePath = "pack://application:,,,/Assets/nvidia-logo-black.png";
            else if (GpuName.Contains("amd", StringComparison.CurrentCultureIgnoreCase) || GpuName.Contains("radeon", StringComparison.CurrentCultureIgnoreCase))
                GpuImagePath = "pack://application:,,,/Assets/amd-logo-black.png";
            else if (GpuName.Contains("intel", StringComparison.CurrentCultureIgnoreCase))
                GpuImagePath = "pack://application:,,,/Assets/intel-logo-black.png";
        }
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void PollCycle()
    {
        if (_hardwareMonitorService == null)
            return;

        _hardwareMonitorService.Update();

        MbName = _hardwareMonitorService.GetMotherboardName();

        CpuName = _hardwareMonitorService.GetCpuName();
        CpuTemp = _hardwareMonitorService.GetCpuTemp();
        CpuUsage = _hardwareMonitorService.GetCpuUsage();
        CpuPower = _hardwareMonitorService.GetCpuPowerCurrent();
        CpuPowerMax = _hardwareMonitorService.GetCpuPowerMax();

        GpuName = _hardwareMonitorService.GetGpuName();
        GpuTemp = _hardwareMonitorService.GetGpuTemp();
        GpuUsage = _hardwareMonitorService.GetGpuUsage();
        GpuPower = _hardwareMonitorService.GetGpuPowerCurrent();
        //GpuPowerMax = _hardwareMonitorService.GetGpuPowerMax();

        MemoryUsageGb = _hardwareMonitorService.GetMemoryUsageGb();
        MemoryTotalGb = _hardwareMonitorService.GetMemoryTotalGb();
        float usedMemoryPercent = _hardwareMonitorService.GetMemoryUsagePercent();
        MemoryUsageDetails = $"{usedMemoryPercent:F0}% ({MemoryUsageGb:F1} GB / {MemoryTotalGb:F1} GB)";
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => PollCycle();

    // ------------------------------------------------------------------------------------------------
    // PropertyChanged event -- used to trigger UI updates

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // ------------------------------------------------------------------------------------------------
    // Dispose
    // ------------------------------------------------------------------------------------------------

    public void Dispose()
    {
        _timer?.Dispose();
        _hardwareMonitorService?.Dispose();
    }
}
