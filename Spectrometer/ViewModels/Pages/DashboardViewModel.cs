using Spectrometer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace Spectrometer.ViewModels.Pages;

public partial class DashboardViewModel : INotifyPropertyChanged
{
    // ------------------------------------------------------------------------------------------------
    // Public Fields
    // ------------------------------------------------------------------------------------------------
    #region Public Fields

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

    private readonly HardwareMonitorService _hardwareMonitorService;
    private readonly System.Timers.Timer _timer;

    // ------------------------------------------------------------------------------------------------
    // Constructor + Events
    // ------------------------------------------------------------------------------------------------

    public DashboardViewModel()
    {
        _hardwareMonitorService = new HardwareMonitorService();
        _timer = new(2000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
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
        GpuPowerMax = _hardwareMonitorService.GetGpuPowerMax();

        MemoryUsageGb = _hardwareMonitorService.GetMemoryUsageGb();
        MemoryTotalGb = _hardwareMonitorService.GetMemoryTotalGb();
        float usedMemoryPercent = _hardwareMonitorService.GetMemoryUsagePercent();
        MemoryUsageDetails = $"{usedMemoryPercent:F0}% ({MemoryUsageGb:F1} GB / {MemoryTotalGb:F1} GB)";
    }

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
