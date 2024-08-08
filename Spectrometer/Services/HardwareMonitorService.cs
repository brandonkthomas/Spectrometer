using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Hosting;
using Spectrometer.Extensions;
using Spectrometer.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;

namespace Spectrometer.Services;

// -------------------------------------------------------------------------------------------
/// <summary>
/// Service that uses LibreHardwareMonitorLib to monitor the system's hardware.
/// </summary>
public partial class HardwareMonitorService : ObservableObject, IHostedService
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

    [ObservableProperty]
    private float _cpuHighestClockSpeed;

    [ObservableProperty]
    private float _cpuMaxClockSpeed;

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
    private int _memoryModuleCount;

    [ObservableProperty]
    private double _memoryUsageGb;

    [ObservableProperty]
    private double _memoryTotalGb;

    // -------------------------------------------------------------------------------------------
    // Storage

    [ObservableProperty]
    private int _storageDeviceCount;

    [ObservableProperty]
    private float _storageReadRate;

    [ObservableProperty]
    private float _storageWriteRate;

    // -------------------------------------------------------------------------------------------
    // Network

    [ObservableProperty]
    private float _networkDownloadUsage;

    [ObservableProperty]
    private float _networkUploadUsage;

    // -------------------------------------------------------------------------------------------
    // Private Fields (for use by the service only)
    // -------------------------------------------------------------------------------------------

    private readonly Computer _computer; // required for LibreHardwareMonitorLib

    private HardwareType _gpuType;
    private string _activeNetworkDeviceName = string.Empty;

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
            try
            {
                foreach (IHardware? hardware in computer.Hardware)
                    hardware.Accept(this);
            }
            catch (Exception ex)
            {
                Logger.WriteExc(ex);
            }
        }

        public void VisitHardware(IHardware hardware)
        {
            try
            {
                hardware.Update();

                foreach (ISensor? sensor in hardware.Sensors)
                    sensor.Accept(this);

                foreach (IHardware? subHardware in hardware.SubHardware)
                    subHardware.Accept(this);
            }
            catch (Exception ex)
            {
                Logger.WriteExc(ex);
            }
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
            // Determine Active Network Device Name

            _activeNetworkDeviceName = GetActiveNetworkDeviceName();

            Logger.Write($"Active Network Device Name: {_gpuType}");

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

    /// <summary>
    /// 
    /// </summary>
    public void Update() => _computer.Accept(new UpdateVisitor());

    /// <summary>
    /// Release the lock on LibreHardwareMonitorLib's WinRing0 sys driver
    /// </summary>
    public void Dispose()
    {
        Logger.Write("HardwareMonitorService disposing...");
        _computer.Close();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _computer.Close();
        return Task.CompletedTask;
    }

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

        AddSensorsToCollection(MbSensors, GetSensorsByHardwareType(HardwareType.Motherboard));
        AddSensorsToCollection(CpuSensors, GetSensorsByHardwareType(HardwareType.Cpu));
        AddSensorsToCollection(GpuSensors, GetSensorsByHardwareType(_gpuType));
        AddSensorsToCollection(MemorySensors, GetSensorsByHardwareType(HardwareType.Memory));
        AddSensorsToCollection(StorageSensors, GetSensorsByHardwareType(HardwareType.Storage));
        AddSensorsToCollection(NetworkSensors, GetSensorsByHardwareType(HardwareType.Network));
        AddSensorsToCollection(FanSensors, GetSensorsByHardwareType(HardwareType.Cooler));
        AddSensorsToCollection(PsuSensors, GetSensorsByHardwareType(HardwareType.Psu));

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
    
    /// <summary>
    /// Helper method to get sensors for a specific hardware type
    /// </summary>
    /// <param name="hardwareType"></param>
    /// <returns></returns>
    private IEnumerable<ISensor> GetSensorsByHardwareType(HardwareType hardwareType)
    {
        return _computer.Hardware
            .Where(h => h.HardwareType == hardwareType)
            .SelectMany(h => h.Sensors ?? []);
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

        HashSet<Identifier>? existingIdentifiers = [];

        foreach (ISensor? sensor in sensors)
        {
            HardwareSensor? modifiedSensor = new(sensor);

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

        ObservableCollection<HardwareSensor>? mbSensors = MbSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? cpuSensors = CpuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? gpuSensors = GpuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? memorySensors = MemorySensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? storageSensors = StorageSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? networkSensors = NetworkSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? fanSensors = FanSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cooler)?.Sensors ?? []);
        ObservableCollection<HardwareSensor>? psuSensors = PsuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Psu)?.Sensors ?? []);

        // maybe need to add allsensors too? idk

        MbSensors = new(mbSensors ?? []);
        CpuSensors = new(cpuSensors ?? []);
        GpuSensors = new(gpuSensors ?? []);
        MemorySensors = new(memorySensors ?? []);
        StorageSensors = new(storageSensors ?? []);
        NetworkSensors = new(networkSensors ?? []);
        FanSensors = new(fanSensors ?? []);
        PsuSensors = new(psuSensors ?? []);
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
            CpuPowerMax = 125; // temporarily hardcoded; LHWL does not expose max supported TDP
            CpuHighestClockSpeed = GetCpuHighestClockSpeed();
            CpuMaxClockSpeed = 5500; // temporarily hardcoded; LHWL does not expose max supported clock speed

            GpuName = GetGpuName();
            GpuTemp = GetGpuTemp();
            GpuUsage = GetGpuUsage();
            GpuMemoryTotal = GetGpuMemoryTotal();
            GpuMemoryUsage = GetGpuMemoryUsage();
            GpuPower = GetGpuPowerCurrent();
            GpuPowerMax = 450; // temporarily hardcoded; LHWL does not expose max TDP

            MemoryModuleCount = GetMemoryModuleCount();
            MemoryUsageGb = GetMemoryUsageGb();
            MemoryTotalGb = GetMemoryTotalGb();
            MemoryUsageDetails = $"{GetMemoryUsagePercent():F0}% ({MemoryUsageGb:F1} GB / {MemoryTotalGb:F1} GB)";

            NetworkDownloadUsage = GetNetworkDownloadUsage();
            NetworkUploadUsage = GetNetworkUploadUsage();

            StorageDeviceCount = GetStorageDeviceCount();
            StorageReadRate = GetStorageReadRate();
            StorageWriteRate = GetStorageWriteRate();
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

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetMotherboardName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Name ?? "Unknown";

    // -------------------------------------------------------------------------------------------
    // CPU

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetCpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Name ?? "Unknown";

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetCpuTemp()
    {
        IHardware? cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        var cpuTempSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);

        return cpuTempSensors.FirstOrDefault(s => s.Name.Contains("Package") || s.Name.Contains("Core (Tctl/Tdie)"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetCpuUsage()
    {
        IHardware? cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Total") || s.Name.Contains("Package")))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetCpuPowerCurrent()
    {
        IHardware? cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetCpuHighestClockSpeed()
    {
        IHardware? cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        var cpuTempSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Clock);

        return cpuTempSensors.Where(s => s.Name.Contains("Core")).Max(s => s.Value) ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // GPU

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetGpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType)?.Name ?? "Unknown";

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetGpuTemp()
    {
        IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? float.NaN;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetGpuUsage()
    {
        IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetGpuMemoryTotal()
    {
        IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Total"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetGpuMemoryUsage()
    {
        IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Used") && s.Name.Contains("GPU"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetGpuPowerCurrent()
    {
        IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // Memory

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static int GetMemoryModuleCount()
    {
        int count = 0;
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");

            foreach (ManagementObject memoryModule in searcher.Get())
                count++;
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
        return count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public double GetMemoryTotalGb()
    {
        float totalMemory = 0;
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
            {
                foreach (var item in searcher.Get())
                    totalMemory += Convert.ToSingle(item["Capacity"]);
            }
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
        return totalMemory / (1024 * 1024 * 1024); // bytes to GB
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public double GetMemoryUsageGb()
    {
        IHardware? mem = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (mem == null)
            return double.NaN;

        ISensor? usedMemorySensor = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used") && !s.Name.Contains("Virtual"));
        if (usedMemorySensor == null)
            return double.NaN;

        return usedMemorySensor.Value.GetValueOrDefault(); // GB
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetMemoryUsagePercent() // percentage
    {
        IHardware? mem = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (mem == null)
            return float.NaN;

        return mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // Storage

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int GetStorageDeviceCount()
    {
        int count = 0;
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
            {
                foreach (var diskDrive in searcher.Get())
                    count++;
            }
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
        return count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetStorageReadRate()
    {
        IHardware? storage = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage);
        if (storage == null)
            return float.NaN;

        return storage.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Read Activity"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetStorageWriteRate()
    {
        IHardware? storage = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage);
        if (storage == null)
            return float.NaN;

        return storage.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Write Activity"))?.Value ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // Network

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetNetworkDownloadUsage()
    {
        IHardware? net = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && h.Name.Contains(_activeNetworkDeviceName))
            ?? _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && !h.Name.Contains("Bluetooth") && !h.Name.Contains("Local Area"));

        if (net == null)
            return float.NaN;

        // Divide bytes/sec by 1000 to get KB/sec
        return (net.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Download Speed"))?.Value) / 1000 ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetNetworkUploadUsage()
    {
        IHardware? net = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && h.Name.Contains(_activeNetworkDeviceName))
            ?? _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && !h.Name.Contains("Bluetooth") && !h.Name.Contains("Local Area"));

        if (net == null)
            return float.NaN;

        // Divide bytes/sec by 1000 to get KB/sec
        return (net.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Upload Speed"))?.Value) / 1000 ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private static string GetActiveNetworkDeviceName()
    {
        try
        {
            NetworkInterface[]? interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface? networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    // Check if this interface has a gateway assigned (means it has an internet connection, more than likely)
                    IPInterfaceProperties? ipProperties = networkInterface.GetIPProperties();

                    if (ipProperties.GatewayAddresses.Any(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                        return networkInterface.Name;
                }
            }

            return "";
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
            return "";
        }
    }
}
