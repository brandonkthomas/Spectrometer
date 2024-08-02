using LibreHardwareMonitor.Hardware;
using Spectrometer.Extensions;
using Spectrometer.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;

namespace Spectrometer.Services;

// -------------------------------------------------------------------------------------------
/// <summary>
/// Service that uses LibreHardwareMonitorLib to monitor the system's hardware.
/// </summary>
public partial class HardwareMonitorService : ObservableObject, IDisposable
{
    // -------------------------------------------------------------------------------------------
    // Sensor Collections
    // -------------------------------------------------------------------------------------------

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _allSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _mbSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _cpuSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _gpuSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _memorySensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _storageSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _networkSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _fanSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _psuSensors = [];

    // -------------------------------------------------------------------------------------------
    // Individual Sensors (used for quick binding access in XAML)
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    // Motherboard

    [ObservableProperty]
    private string _mbName = string.Empty;

    // -------------------------------------------------------------------------------------------
    // CPU

    [ObservableProperty]
    private string _cpuName = string.Empty;

    [ObservableProperty]
    private float _cpuTemp;

    [ObservableProperty]
    private float _cpuUsage;

    [ObservableProperty]
    private float _cpuPower;

    [ObservableProperty]
    private float _cpuPowerMax;

    // -------------------------------------------------------------------------------------------
    // GPU

    [ObservableProperty]
    private string _gpuName = string.Empty;

    [ObservableProperty]
    private float _gpuTemp;

    [ObservableProperty]
    private float _gpuUsage;

    [ObservableProperty]
    private float _gpuMemoryTotal;

    [ObservableProperty]
    private float _gpuMemoryUsage;

    [ObservableProperty]
    private float _gpuPower;

    [ObservableProperty]
    private float _gpuPowerMax;

    // -------------------------------------------------------------------------------------------
    // Memory

    [ObservableProperty]
    private string _memoryUsageDetails = string.Empty;

    [ObservableProperty]
    private double _memoryUsageGb;

    [ObservableProperty]
    private double _memoryTotalGb;

    // -------------------------------------------------------------------------------------------
    // Private Fields (for use by the service only)
    // -------------------------------------------------------------------------------------------

    private readonly Computer _computer; // required for LibreHardwareMonitorLib

    private HardwareType _gpuType;

    private bool _isInitialized = false;

    // -------------------------------------------------------------------------------------------
    // Singleton Support for HW Monitor Service (allows sharing of the same instance across the application)

    private static readonly Lazy<HardwareMonitorService> _instance = new(() => new HardwareMonitorService());
    public static HardwareMonitorService Instance => _instance.Value;

    // -------------------------------------------------------------------------------------------
    // Update Visitor (internal-only) -- Required for LibreHardwareMonitorLib

    private class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            foreach (var hardware in computer.Hardware)
                hardware.Accept(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();

            foreach (var sensor in hardware.Sensors)
                sensor.Accept(this);

            foreach (var subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }

    // -------------------------------------------------------------------------------------------
    // Constructor + Events
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public HardwareMonitorService()
    {
        Logger.Write("HardwareMonitorService initializing...");

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
        };

        try
        {
            _computer.Open();

            // -------------------------------------------------------------------------------------------
            // Determine GPU Type

            if (_computer.Hardware.Any(h => h.HardwareType == HardwareType.GpuNvidia))
                _gpuType = HardwareType.GpuNvidia;
            else if (_computer.Hardware.Any(h => h.HardwareType == HardwareType.GpuAmd))
                _gpuType = HardwareType.GpuAmd;
            else
                _gpuType = HardwareType.GpuIntel;

            Logger.Write($"GPU Type: {_gpuType}");

            // -------------------------------------------------------------------------------------------
            // Poll sensors once before MainWindow timer to get initial values

            Stopwatch stopwatch = new();
            stopwatch.Start();

            Update();
            InitializeAllSensors();
            PollSpecificSensors();

            stopwatch.Stop();
            Logger.Write($"Sensors polled in {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }

        Logger.Write("HardwareMonitorService initialized");
    }

    public void Update() => _computer.Accept(new UpdateVisitor());

    public void Dispose() => _computer.Close();

    // -------------------------------------------------------------------------------------------
    // Sensor Collections
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Initialize sensors on startup
    /// </summary>
    public void InitializeAllSensors()
    {
        if (_isInitialized) return;

        AddSensorsToCollection(MbSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Sensors ?? []);
        AddSensorsToCollection(CpuSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Sensors ?? []);
        AddSensorsToCollection(GpuSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType)?.Sensors ?? []);
        AddSensorsToCollection(MemorySensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory)?.Sensors ?? []);
        AddSensorsToCollection(StorageSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage)?.Sensors ?? []);
        AddSensorsToCollection(NetworkSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network)?.Sensors ?? []);
        AddSensorsToCollection(FanSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cooler)?.Sensors ?? []);
        AddSensorsToCollection(PsuSensors, _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Psu)?.Sensors ?? []);

        // Combine all sensors into AllSensors
        AllSensors = new ObservableCollection<HardwareSensor>(MbSensors ?? new ObservableCollection<HardwareSensor>()
            .Concat(CpuSensors ?? [])
            .Concat(GpuSensors ?? [])
            .Concat(MemorySensors ?? [])
            .Concat(StorageSensors ?? [])
            .Concat(NetworkSensors ?? [])
            .Concat(FanSensors ?? [])
            .Concat(PsuSensors ?? []));

        Logger.Write($"{AllSensors.Count} total system sensors");
        Logger.Write($"{MbSensors?.Count.ToString() ?? "Error retrieving"} motherboard sensors");
        Logger.Write($"{CpuSensors?.Count.ToString() ?? "Error retrieving"} CPU sensors");
        Logger.Write($"{GpuSensors?.Count.ToString() ?? "Error retrieving"} GPU sensors");
        Logger.Write($"{MemorySensors?.Count.ToString() ?? "Error retrieving"} memory sensors");
        Logger.Write($"{StorageSensors?.Count.ToString() ?? "Error retrieving"} storage sensors");
        Logger.Write($"{NetworkSensors?.Count.ToString() ?? "Error retrieving"} network sensors");
        Logger.Write($"{FanSensors?.Count.ToString() ?? "Error retrieving"} fan sensors");
        Logger.Write($"{PsuSensors?.Count.ToString() ?? "Error retrieving"} PSU sensors");

        _isInitialized = true;
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="sensors"></param>
    private void AddSensorsToCollection(ObservableCollection<HardwareSensor>? collection, IEnumerable<ISensor> sensors)
    {
        if (collection == null) return;

        var existingIdentifiers = new HashSet<Identifier>();

        foreach (var sensor in sensors)
        {
            var modifiedSensor = new HardwareSensor(sensor);

            // Fix identifier if it isn't unique (no clue why this happens... probably LibreHardwareMonitorLib's fault)
            if (existingIdentifiers.Contains(modifiedSensor.Identifier))
                //modifiedSensor.Identifier = new Identifier($"{modifiedSensor.Identifier}0"); // this throws an "Invalid Identifier" exception
                continue; // just ignore for now. idk why this happens

            existingIdentifiers.Add(modifiedSensor.Identifier);
            collection.Add(modifiedSensor);
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Poll sensors to update their values
    /// </summary>
    public void PollAllSensors()
    {
        if (!_isInitialized) return;

        // I couldnt get UI refresh events to work so I resorted to just resetting the collections
        // every time (but keeping the app-specific properties like IsPinned and IsGraphEnabled).
        // It's 12am and I'm tired. This will never get fixed.

        var mbSensors = MbSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Sensors ?? []);
        var cpuSensors = CpuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Sensors ?? []);
        var gpuSensors = GpuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType)?.Sensors ?? []);
        var memorySensors = MemorySensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory)?.Sensors ?? []);
        var storageSensors = StorageSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage)?.Sensors ?? []);
        var networkSensors = NetworkSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network)?.Sensors ?? []);
        var fanSensors = FanSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cooler)?.Sensors ?? []);
        var psuSensors = PsuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Psu)?.Sensors ?? []);
        // maybe need to add allsensors too? idk

        MbSensors = new ObservableCollection<HardwareSensor>(mbSensors ?? []);
        CpuSensors = new ObservableCollection<HardwareSensor>(cpuSensors ?? []);
        GpuSensors = new ObservableCollection<HardwareSensor>(gpuSensors ?? []);
        MemorySensors = new ObservableCollection<HardwareSensor>(memorySensors ?? []);
        StorageSensors = new ObservableCollection<HardwareSensor>(storageSensors ?? []);
        NetworkSensors = new ObservableCollection<HardwareSensor>(networkSensors ?? []);
        FanSensors = new ObservableCollection<HardwareSensor>(fanSensors ?? []);
        PsuSensors = new ObservableCollection<HardwareSensor>(psuSensors ?? []);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Poll specific sensors (used for quick binding access in XAML)
    /// </summary>
    public void PollSpecificSensors()
    {
        try
        {
            MbName = GetMotherboardName();

            CpuName = GetCpuName();
            CpuTemp = GetCpuTemp();
            CpuUsage = GetCpuUsage();
            CpuPower = GetCpuPowerCurrent();
            CpuPowerMax = GetCpuPowerMax();

            GpuName = GetGpuName();
            GpuTemp = GetGpuTemp();
            GpuUsage = GetGpuUsage();
            GpuMemoryTotal = GetGpuMemoryTotal();
            GpuMemoryUsage = GetGpuMemoryUsage();
            GpuPower = GetGpuPowerCurrent();
            GpuPowerMax = GetGpuPowerMax();

            MemoryUsageGb = GetMemoryUsageGb();
            MemoryTotalGb = GetMemoryTotalGb();
            MemoryUsageDetails = $"{GetMemoryUsagePercent():F0}% ({MemoryUsageGb:F1} GB / {MemoryTotalGb:F1} GB)";
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }

    // -------------------------------------------------------------------------------------------
    // Specific Sensors
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    // Motherboard

    public string GetMotherboardName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Name ?? "Unknown";

    // -------------------------------------------------------------------------------------------
    // CPU

    public string GetCpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Name ?? "Unknown";

    public float GetCpuTemp()
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        var cpuTempSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);

        return cpuTempSensors.FirstOrDefault(s => s.Name.Contains("Package") || s.Name.Contains("Core (Tctl/Tdie)"))?.Value ?? float.NaN;
    }

    public float GetCpuUsage()
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Total") || s.Name.Contains("Package")))?.Value ?? float.NaN;
    }

    public float GetCpuPowerCurrent()
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    public float GetCpuPowerMax()
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Max ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // GPU

    public string GetGpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType)?.Name ?? "Unknown";

    public float GetGpuTemp()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? float.NaN;
    }

    public float GetGpuUsage()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    public float GetGpuMemoryTotal()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Total"))?.Value ?? float.NaN;
    }

    public float GetGpuMemoryUsage()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Used") && s.Name.Contains("GPU"))?.Value ?? float.NaN;
    }

    public float GetGpuPowerCurrent()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    public float GetGpuPowerMax()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Max ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // Memory

    public double GetMemoryTotalGb()
    {
        float totalMemory = 0;
        using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
        {
            foreach (var item in searcher.Get())
                totalMemory += Convert.ToSingle(item["Capacity"]);
        }

        return totalMemory / (1024 * 1024 * 1024); // bytes to GB
    }

    public double GetMemoryUsageGb()
    {
        var mem = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (mem == null)
            return double.NaN;

        var usedMemorySensor = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used") && !s.Name.Contains("Virtual"));
        if (usedMemorySensor == null)
            return double.NaN;

        return usedMemorySensor.Value.GetValueOrDefault(); // GB
    }

    public float GetMemoryUsagePercent() // percentage
    {
        var mem = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (mem == null)
            return float.NaN;

        return mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // Storage

    // TODO (do we even need any of these for Dashboard/UI (other than sensor dropdowns)? probably not)
}
