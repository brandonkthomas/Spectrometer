using LibreHardwareMonitor.Hardware;
using Spectrometer.Models;
using System.Management;

namespace Spectrometer.Services;

public partial class HardwareMonitorService
{
    // -------------------------------------------------------------------------------------------
    // This support file houses specialized methods for querying data from specific hardware
    // components. Called by the HardwareMonitorService class.
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    // Motherboard

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GetMotherboardName(Computer computer) => computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard)?.Name ?? "Unknown";

    // -------------------------------------------------------------------------------------------
    // CPU

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GetCpuName(Computer computer) => computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Name ?? "Unknown";

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetCpuTemp(Computer computer)
    {
        IHardware? cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        var cpuTempSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature);

        return cpuTempSensors.FirstOrDefault(s => s.Name.Contains("Package") || s.Name.Contains("Core (Tctl/Tdie)"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetCpuUsage(Computer computer)
    {
        IHardware? cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Total") || s.Name.Contains("Package")))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetCpuPowerCurrent(Computer computer)
    {
        IHardware? cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (cpu == null)
            return float.NaN;

        return cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetCpuHighestClockSpeed(Computer computer)
    {
        IHardware? cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
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
    public static string GetGpuName(Computer computer, HardwareType gpuType) => computer.Hardware.FirstOrDefault(h => h.HardwareType == gpuType)?.Name ?? "Unknown";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="computer"></param>
    /// <returns></returns>
    public static HardwareType GetGpuType(Computer computer)
    {
        if (computer.Hardware.Any(h => h.HardwareType == HardwareType.GpuNvidia))
            return HardwareType.GpuNvidia;
        else if (computer.Hardware.Any(h => h.HardwareType == HardwareType.GpuAmd))
            return HardwareType.GpuAmd;
        else
            return HardwareType.GpuIntel;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetGpuTemp(Computer computer, HardwareType gpuType)
    {
        IHardware? gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetGpuUsage(Computer computer, HardwareType gpuType)
    {
        IHardware? gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == gpuType);
        if (gpu == null)
            return float.NaN;

        return gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetGpuMemoryTotal(Computer computer, HardwareType gpuType)
    {
        IHardware? gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == gpuType);
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Total"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetGpuMemoryUsage(Computer computer, HardwareType gpuType)
    {
        IHardware? gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == gpuType);
        if (gpu == null)
            return float.NaN;

        var gpuMemSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.SmallData);

        return gpuMemSensors.FirstOrDefault(s => s.Name.Contains("Used") && s.Name.Contains("GPU"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetGpuPowerCurrent(Computer computer, HardwareType gpuType)
    {
        IHardware? gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == gpuType);
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
    public static int GetMemoryModuleCount(Computer computer)
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
    public static double GetMemoryTotalGb()
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
    public static double GetMemoryUsageGb(Computer computer)
    {
        IHardware? mem = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
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
    public static float GetMemoryUsagePercent(Computer computer) // percentage
    {
        IHardware? mem = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
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
    public static int GetStorageDeviceCount()
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
    public static float GetStorageReadRate(Computer computer)
    {
        IHardware? storage = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage);
        if (storage == null)
            return float.NaN;

        return storage.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Read Activity"))?.Value ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetStorageWriteRate(Computer computer)
    {
        IHardware? storage = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage);
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
    public static float GetNetworkDownloadUsage(Computer computer, string activeNetworkDeviceName)
    {
        IHardware? net = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && h.Name.Contains(activeNetworkDeviceName))
            ?? computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && !h.Name.Contains("Bluetooth") && !h.Name.Contains("Local Area"));

        if (net == null)
            return float.NaN;

        // Divide bytes/sec by 1000 to get KB/sec
        return (net.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Download Speed"))?.Value) / 1000 ?? float.NaN;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static float GetNetworkUploadUsage(Computer computer, string activeNetworkDeviceName)
    {
        IHardware? net = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && h.Name.Contains(activeNetworkDeviceName))
            ?? computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Network && !h.Name.Contains("Bluetooth") && !h.Name.Contains("Local Area"));

        if (net == null)
            return float.NaN;

        // Divide bytes/sec by 1000 to get KB/sec
        return (net.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Upload Speed"))?.Value) / 1000 ?? float.NaN;
    }
}
