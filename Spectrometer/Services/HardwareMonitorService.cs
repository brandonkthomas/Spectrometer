using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Management;

namespace Spectrometer.Services;

// ------------------------------------------------------------------------------------------------
/// <summary>
/// Service that uses LibreHardwareMonitorLib to monitor the system's hardware.
/// </summary>
public class HardwareMonitorService : IDisposable
{
    // ------------------------------------------------------------------------------------------------
    // Constructor + Support
    // ------------------------------------------------------------------------------------------------

    private readonly Computer _computer;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
        };

        _computer.Open();
    }

    public void Update() => _computer.Accept(new UpdateVisitor());

    public void Dispose() => _computer.Close();

    // ------------------------------------------------------------------------------------------------
    // Value Retrieval Functions
    // ------------------------------------------------------------------------------------------------

    // ------------------------------------------------------------------------------------------------
    // Motherboard

    public string GetMotherboardName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Name ?? "Unknown";

    // ------------------------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------------------------
    // GPU

    public string GetGpuName() => _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia)?.Name ?? "Unknown";

    public float GetGpuTemp()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? float.NaN;
    }

    public float GetGpuUsage()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    public float GetGpuPowerCurrent()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    public float GetGpuPowerMax()
    {
        var gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Max ?? float.NaN;
    }

    // ------------------------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------------------------
    // Update Visitor (internal-only)
    // ------------------------------------------------------------------------------------------------

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
