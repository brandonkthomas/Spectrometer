using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Hosting;
using Spectrometer.Extensions;
using Spectrometer.Helpers;
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
    // Sensor Collections (Storage)
    // -------------------------------------------------------------------------------------------

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _allSensors = [];

    [ObservableProperty]
    private ObservableCollection<HardwareSensor>? _pinnedSensors = [];

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
    private ObservableCollection<HardwareSensor>? _controllerSensors = [];

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
    // Application-Wide Support for HW Monitor Service (allows sharing of the same instance
    // + events across the application)

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
            IsStorageEnabled = true,
            //IsPsuEnabled = true,
            //IsControllerEnabled = true
        };

        try
        {
            _computer.Open();

            // -------------------------------------------------------------------------------------------
            // Determine GPU Type

            _gpuType = GetGpuType(_computer);
            Logger.Write($"GPU Type: {_gpuType}");

            // -------------------------------------------------------------------------------------------
            // Determine Active Network Device Name

            _activeNetworkDeviceName = NetworkDeviceHelper.GetActiveNetworkDeviceName();
            Logger.Write($"Active Network Device Name: {_activeNetworkDeviceName}");

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
        Logger.Write("HardwareMonitorService stopping...");
        _computer.Close();
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------------------------
    // Sensor Collections (Initialization)
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Initialize sensors once on startup. 
    /// This method cannot be called after initialization.
    /// </summary>
    public void InitializeAllSensors()
    {
        if (_isInitialized) return;

        // Get all sensors first (doesnt seem to work with the checkboxes on Sensors tab)
        //AddSensorsToCollection(AllSensors, _computer.Hardware.SelectMany(h => h.Sensors));

        // Group by type next
        AddSensorsToCollection(MbSensors, GetSensorsByHardwareType(HardwareType.Motherboard));
        AddSensorsToCollection(CpuSensors, GetSensorsByHardwareType(HardwareType.Cpu));
        AddSensorsToCollection(GpuSensors, GetSensorsByHardwareType(_gpuType));
        AddSensorsToCollection(MemorySensors, GetSensorsByHardwareType(HardwareType.Memory));
        AddSensorsToCollection(StorageSensors, GetSensorsByHardwareType(HardwareType.Storage));
        AddSensorsToCollection(NetworkSensors, GetSensorsByHardwareType(HardwareType.Network));
        AddSensorsToCollection(ControllerSensors, GetSensorsByHardwareType(HardwareType.EmbeddedController));
        AddSensorsToCollection(PsuSensors, GetSensorsByHardwareType(HardwareType.Psu));

        // Combine all sensors into AllSensors
        var _mbSensors = MbSensors ?? [];

        AllSensors = new ObservableCollection<HardwareSensor>(
            _mbSensors.Concat(CpuSensors ?? [])
                .Concat(GpuSensors ?? [])
                .Concat(MemorySensors ?? [])
                .Concat(StorageSensors ?? [])
                .Concat(NetworkSensors ?? [])
                .Concat(ControllerSensors ?? [])
                .Concat(PsuSensors ?? [])) ?? [];

        // Load any previously pinned or graphed sensors from AppSettings
        LoadPinnedSensorsFromSettings();
        LoadGraphedSensorsFromSettings();

        // Log final sensor counts
        Logger.Write($"{AllSensors?.Count.ToString() ?? "Error retrieving"} total system sensors");
        Logger.Write($"{PinnedSensors?.Count.ToString() ?? "Error retrieving"} user-pinned sensors");
        Logger.Write($"{MbSensors?.Count.ToString() ?? "Error retrieving"} motherboard sensors");
        Logger.Write($"{CpuSensors?.Count.ToString() ?? "Error retrieving"} CPU sensors");
        Logger.Write($"{GpuSensors?.Count.ToString() ?? "Error retrieving"} GPU sensors");
        Logger.Write($"{MemorySensors?.Count.ToString() ?? "Error retrieving"} memory sensors");
        Logger.Write($"{StorageSensors?.Count.ToString() ?? "Error retrieving"} storage sensors");
        Logger.Write($"{NetworkSensors?.Count.ToString() ?? "Error retrieving"} network sensors");
        Logger.Write($"{ControllerSensors?.Count.ToString() ?? "Error retrieving"} embedded controller sensors");
        Logger.Write($"{PsuSensors?.Count.ToString() ?? "Error retrieving"} PSU sensors");

        _isInitialized = true;
    }

    /// <summary>
    /// Helper method to get pinned sensors from AppSettings & populate local PinnedSensors collection.
    /// </summary>
    private void LoadPinnedSensorsFromSettings()
    {
        var pinnedSensorIdentifiers = App.SettingsMgr?.Settings?.PinnedSensorIdentifiers;

        if (pinnedSensorIdentifiers != null && pinnedSensorIdentifiers.Count > 0)
        {
            var pinnedSensorsFromConfig = pinnedSensorIdentifiers
                .Select(id => AllSensors?.FirstOrDefault(s => s.Identifier.ToString() == id))
                .Where(s => s != null)
                .Cast<ISensor>();

            // Update AllSensors with saved Pinned statuses from AppSettings
            if (AllSensors != null)
            {
                foreach (var id in pinnedSensorIdentifiers)
                {
                    var matchingSensorHandle = AllSensors.FirstOrDefault(s => s.Identifier.ToString() == id);
                    if (matchingSensorHandle == null) continue;
                    matchingSensorHandle.IsPinned = true;
                }
            }

            Logger.Write($"HardwareMonitorService: {pinnedSensorsFromConfig.Count()} pinned sensor(s) found in settings & matched to available system sensors");

            AddSensorsToCollection(PinnedSensors, pinnedSensorsFromConfig);
        }
    }

    /// <summary>
    /// Helper method to get graphed sensors from AppSettings & override local IsGraphEnabled property in matching AllSensors sensors.
    /// </summary>
    private void LoadGraphedSensorsFromSettings()
    {
        if (AllSensors == null)
            return;

        var graphedSensorIdentifiers = App.SettingsMgr?.Settings?.GraphedSensorIdentifiers;

        // Update AllSensors with saved Graphed statuses from AppSettings
        if (graphedSensorIdentifiers != null)
        {
            int count = 0;
            foreach (var id in graphedSensorIdentifiers)
            {
                var matchingSensorHandle = AllSensors.FirstOrDefault(s => s.Identifier.ToString() == id);
                if (matchingSensorHandle == null) continue;
                matchingSensorHandle.IsGraphEnabled = true;

                count += 1;
            }

            Logger.Write($"HardwareMonitorService: {count} graphed sensor(s) found in settings & matched to available system sensors");
        }
    }

    /// <summary>
    /// Helper method to get sensors for a specific hardware type (reduces boilerplate above).
    /// Checks from AllSensors first, then falls back to _computer.Hardware
    /// </summary>
    /// <param name="hardwareType"></param>
    private IEnumerable<ISensor> GetSensorsByHardwareType(HardwareType hardwareType)
    {
        //if (AllSensors != null && AllSensors.Count > 0)
        //    return AllSensors.Where(s => s.Hardware.HardwareType == hardwareType);
        //else
            return _computer.Hardware
                .Where(h => h.HardwareType == hardwareType)
                .SelectMany(h => h.Sensors ?? []);
    }

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
    // All (& Pinned) Sensors
    // -------------------------------------------------------------------------------------------

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

        //ObservableCollection<HardwareSensor>? allSensors = AllSensors?.UpdateSensorCollection(_computer.Hardware.SelectMany(h => h.Sensors));
        //ObservableCollection<HardwareSensor>? mbSensors = MbSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? cpuSensors = CpuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? gpuSensors = GpuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == _gpuType)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? memorySensors = MemorySensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? storageSensors = StorageSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? networkSensors = NetworkSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? controllerSensors = ControllerSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.EmbeddedController)?.Sensors ?? []);
        //ObservableCollection<HardwareSensor>? psuSensors = PsuSensors?.UpdateSensorCollection(_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Psu)?.Sensors ?? []);

        var allSensors = AllSensors?.UpdateSensorCollection(_computer.Hardware.SelectMany(h => h.Sensors));
        var mbSensors = MbSensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.Motherboard));
        var cpuSensors = CpuSensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.Cpu));
        var gpuSensors = GpuSensors?.UpdateSensorCollection(GetSensorsByHardwareType(_gpuType));
        var memorySensors = MemorySensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.Memory));
        var storageSensors = StorageSensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.Storage));
        var networkSensors = NetworkSensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.Network));
        var controllerSensors = ControllerSensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.EmbeddedController));
        var psuSensors = PsuSensors?.UpdateSensorCollection(GetSensorsByHardwareType(HardwareType.Psu));

        AllSensors = new(allSensors ?? []);
        PinnedSensors = new(allSensors?.Where(s => s.IsPinned) ?? []);
        MbSensors = new(mbSensors ?? []);
        CpuSensors = new(cpuSensors ?? []);
        GpuSensors = new(gpuSensors ?? []);
        MemorySensors = new(memorySensors ?? []);
        StorageSensors = new(storageSensors ?? []);
        NetworkSensors = new(networkSensors ?? []);
        ControllerSensors = new(controllerSensors ?? []);
        PsuSensors = new(psuSensors ?? []);

        UpdatePinnedSensorsInSettings();
    }

    /// <summary>
    /// 
    /// </summary>
    private void UpdatePinnedSensorsInSettings()
    {
        AppSettings? config = App.SettingsMgr?.Settings;
        if (config == null)
            return;

        List<string> currentPinnedIdentifiers = PinnedSensors?.Select(sensor => sensor.Identifier.ToString()).ToList() ?? [];
        List<string> settingsPinnedIdentifiers = config.PinnedSensorIdentifiers ?? [];

        if (!currentPinnedIdentifiers.SequenceEqual(settingsPinnedIdentifiers))
        {
            config.PinnedSensorIdentifiers = currentPinnedIdentifiers;
            App.SettingsMgr?.SaveSettings();
        }
    }

    // -------------------------------------------------------------------------------------------
    // Specific Sensors
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Poll specific sensors (used for quick binding access in XAML)
    /// </summary>
    public void PollSpecificSensors()
    {
        try
        {
            MbName = GetMotherboardName(_computer);

            CpuName = GetCpuName(_computer);
            CpuTemp = GetCpuTemp(_computer);
            CpuUsage = GetCpuUsage(_computer);
            CpuPower = GetCpuPowerCurrent(_computer);
            CpuPowerMax = 125; // temporarily hardcoded; LHWL does not expose max supported TDP
            CpuHighestClockSpeed = GetCpuHighestClockSpeed(_computer);
            CpuMaxClockSpeed = 5500; // temporarily hardcoded; LHWL does not expose max supported clock speed

            GpuName = GetGpuName(_computer, _gpuType);
            GpuTemp = GetGpuTemp(_computer, _gpuType);
            GpuUsage = GetGpuUsage(_computer, _gpuType);
            GpuMemoryTotal = GetGpuMemoryTotal(_computer, _gpuType);
            GpuMemoryUsage = GetGpuMemoryUsage(_computer, _gpuType);
            GpuPower = GetGpuPowerCurrent(_computer, _gpuType);
            GpuPowerMax = 450; // temporarily hardcoded; LHWL does not expose max TDP

            MemoryModuleCount = GetMemoryModuleCount(_computer);
            MemoryUsageGb = GetMemoryUsageGb(_computer);
            MemoryTotalGb = GetMemoryTotalGb();
            MemoryUsageDetails = $"{GetMemoryUsagePercent(_computer):F0}% ({MemoryUsageGb:F1} GB / {MemoryTotalGb:F1} GB)";

            NetworkDownloadUsage = GetNetworkDownloadUsage(_computer, _activeNetworkDeviceName);
            NetworkUploadUsage = GetNetworkUploadUsage(_computer, _activeNetworkDeviceName);

            StorageDeviceCount = GetStorageDeviceCount();
            StorageReadRate = GetStorageReadRate(_computer);
            StorageWriteRate = GetStorageWriteRate(_computer);
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }
}
