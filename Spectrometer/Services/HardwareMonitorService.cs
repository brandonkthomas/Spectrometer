using LibreHardwareMonitor.Hardware;
using Spectrometer.Models;
using System.Collections.ObjectModel;
using System.Management;

namespace Spectrometer.Services;

// -------------------------------------------------------------------------------------------
/// <summary>
/// Service that uses LibreHardwareMonitorLib to monitor the system's hardware.
/// </summary>
public class HardwareMonitorService : IDisposable
{
    // -------------------------------------------------------------------------------------------
    // Singleton Support for HW Monitor Service (allows sharing of the same instance across the application)
    // -------------------------------------------------------------------------------------------

    private static readonly Lazy<HardwareMonitorService> _instance = new(() => new HardwareMonitorService());
    public static HardwareMonitorService Instance => _instance.Value;

    // -------------------------------------------------------------------------------------------
    // Constructor + Events
    // -------------------------------------------------------------------------------------------

    private readonly Computer _computer;

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
            Logger.Write("HardwareMonitorService initialized");
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
        }
    }

    public void Update() => _computer.Accept(new UpdateVisitor());

    public void Dispose() => _computer.Close();

    // -------------------------------------------------------------------------------------------
    // Public Sensor Value Retrieval
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    // Motherboard

    public string GetMotherboardName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Name ?? "Unknown";

    public ObservableCollection<HardwareSensor> GetMotherboardSensors()
    {
        var mb = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard);
        if (mb == null) return [];

        ObservableCollection<HardwareSensor> sensors = [];

        foreach (var sensor in mb.Sensors)
            sensors.Add(new HardwareSensor
            {
                Name = sensor.Name,
                Value = sensor.Value.GetValueOrDefault(),
                Min = sensor.Min.GetValueOrDefault(),
                Max = sensor.Max.GetValueOrDefault()
            });

        return sensors;
    }

    // -------------------------------------------------------------------------------------------
    // CPU

    public string GetCpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Name ?? "Unknown";

    public ObservableCollection<HardwareSensor> GetCpuSensors()
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null) return [];

        ObservableCollection<HardwareSensor> sensors = [];

        foreach (var sensor in cpu.Sensors)
            sensors.Add(new HardwareSensor
            {
                Name = sensor.Name,
                Value = sensor.Value.GetValueOrDefault(),
                Min = sensor.Min.GetValueOrDefault(),
                Max = sensor.Max.GetValueOrDefault(),
                SensorType = sensor.SensorType
            });

        return sensors;
    }

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
#if false
        foreach (var sensor in cpu.Sensors.Where(s => s.SensorType == SensorType.Load))
            Debug.WriteLine($"{sensor.Name}: {sensor.Value}");
#endif
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

    private HardwareType _getGpuType()
    {
        if (_computer.Hardware.Any(h => h.HardwareType == HardwareType.GpuNvidia))
            return HardwareType.GpuNvidia;

        else if (_computer.Hardware.Any(h => h.HardwareType == HardwareType.GpuAmd))
            return HardwareType.GpuAmd;

        else
            return HardwareType.GpuIntel;
    }

    public string GetGpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType())?.Name ?? "Unknown";

    public ObservableCollection<HardwareSensor> GetGpuSensors()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null) return [];

        ObservableCollection<HardwareSensor> sensors = [];

        foreach (var sensor in gpu.Sensors)
            sensors.Add(new HardwareSensor
            {
                Name = sensor.Name,
                Value = sensor.Value.GetValueOrDefault(),
                Min = sensor.Min.GetValueOrDefault(),
                Max = sensor.Max.GetValueOrDefault(),
                SensorType = sensor.SensorType
            });

        return sensors;
    }

    public float GetGpuTemp()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? float.NaN;
    }

    public float GetGpuUsage()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    public float GetGpuMemoryTotal()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Total"))?.Value ?? float.NaN;
    }

    public float GetGpuMemoryUsage()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Used") && s.Name.Contains("GPU"))?.Value ?? float.NaN;
    }

    public float GetGpuPowerCurrent()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    public float GetGpuPowerMax()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == _getGpuType());
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Max ?? float.NaN;
    }

    // -------------------------------------------------------------------------------------------
    // Memory

    public ObservableCollection<HardwareSensor> GetMemorySensors()
    {
        var mem = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (mem == null) return [];

        ObservableCollection<HardwareSensor> sensors = [];

        foreach (var sensor in mem.Sensors)
            sensors.Add(new HardwareSensor
            {
                Name = sensor.Name,
                Value = sensor.Value.GetValueOrDefault(),
                Min = sensor.Min.GetValueOrDefault(),
                Max = sensor.Max.GetValueOrDefault(),
                SensorType = sensor.SensorType
            });

        return sensors;
    }

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

    public ObservableCollection<HardwareSensor> GetStorageSensors()
    {
        var storage = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage);
        if (storage == null) return [];

        ObservableCollection<HardwareSensor> sensors = [];

        foreach (var sensor in storage.Sensors)
            sensors.Add(new HardwareSensor
            {
                Name = sensor.Name,
                Value = sensor.Value.GetValueOrDefault(),
                Min = sensor.Min.GetValueOrDefault(),
                Max = sensor.Max.GetValueOrDefault(),
                SensorType = sensor.SensorType
            });

        return sensors;
    }

    // -------------------------------------------------------------------------------------------
    // Update Visitor (internal-only)
    // -------------------------------------------------------------------------------------------

    private class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            foreach (var hardware in computer.Hardware)
            {
                hardware.Accept(this);
            }
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                sensor.Accept(this);
            }
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
