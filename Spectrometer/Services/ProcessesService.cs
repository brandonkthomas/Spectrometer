using Spectrometer.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Spectrometer.Services;

#if false // Commented out for now. Probably going to scrap the process grid.
public class ProcessesService
{
    private Dictionary<int, PerformanceCounter> cpuCounters;
    private readonly Dictionary<int, TimeSpan> lastCpuTimes;
    private readonly Dictionary<int, DateTime> lastCheckTimes;
    private static readonly Lazy<ProcessesService> _instance = new(() => new ProcessesService());
    public static ProcessesService Instance => _instance.Value;
    //private Dictionary<int, IntPtr> processGpuCounters;
    //private IntPtr queryHandle;


    public ProcessesService()
    {
        cpuCounters = new Dictionary<int, PerformanceCounter>();
        lastCpuTimes = new Dictionary<int, TimeSpan>();
        lastCheckTimes = new Dictionary<int, DateTime>();

        //InitializeGpuCounters();
    }

    public ObservableCollection<ProcessInfo> LoadProcesses()
    {
        ObservableCollection<ProcessInfo> collection = new ObservableCollection<ProcessInfo>();
        var processes = Process.GetProcesses().Where(x => x.ProcessName != "Idle").Select(p => new ProcessInfo
        {
            ProcessName = p.ProcessName,
            ProcessId = p.Id.ToString(),
            CpuUsage = GetCpuUsage(p),
            GpuUsage = GetGpuUsage(p),
            MemoryUsage = p.WorkingSet64 / 1024.0 / 1024.0,
            DownloadUsage = GetNetworkDownload(p),
            UploadUsage = GetNetworkUpload(p)
        }).ToList();

        foreach (var process in processes)
        {
            collection.Add(process);
        }

        return collection;
    }

    //private void InitializeGpuCounters()
    //{
    //    processGpuCounters = new Dictionary<int, IntPtr>();
    //    uint status = PdhHelper.PdhOpenQuery(null, IntPtr.Zero, out queryHandle);
    //    if (status != 0)
    //    {
    //        throw new Exception("PdhOpenQuery failed with status: " + status);
    //    }
    //    var category = new PerformanceCounterCategory("GPU Engine");
    //    var instanceNames = category.GetInstanceNames();

    //    foreach (var instanceName in instanceNames)
    //    {
    //        try
    //        {
    //            var parts = instanceName.Split('_');
    //            if (parts.Length >= 2 && int.TryParse(parts[1], out int processId))
    //            {
    //                if (!processGpuCounters.ContainsKey(processId))
    //                {
    //                    string counterPath = $@"\GPU Engine({instanceName})\Utilization Percentage";
    //                    status = PdhHelper.PdhAddCounter(queryHandle, counterPath, IntPtr.Zero, out IntPtr counterHandle);
    //                    if (status == 0)
    //                    {
    //                        processGpuCounters[processId] = counterHandle;
    //                    }
    //                }
    //            }
    //        }
    //        catch
    //        {
    //            // Handle exceptions if necessary (e.g., invalid instance names)
    //        }
    //    }
    //}

    private double GetCpuUsage(Process process)
    {
        if (!cpuCounters.TryGetValue(process.Id, out var cpuCounter) && !process.HasExited)
        {
            cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
            cpuCounters[process.Id] = cpuCounter;
            lastCpuTimes[process.Id] = process.TotalProcessorTime;
            lastCheckTimes[process.Id] = DateTime.UtcNow;
            return 0; // Initial value will be 0 until next poll interval
        }

        var lastCpuTime = lastCpuTimes[process.Id];
        var lastCheckTime = lastCheckTimes[process.Id];
        var currentCpuTime = process.TotalProcessorTime;
        var currentTime = DateTime.UtcNow;

        // Calculate the CPU usage over the interval
        double cpuUsage = (currentCpuTime.TotalMilliseconds - lastCpuTime.TotalMilliseconds) / currentTime.Subtract(lastCheckTime).TotalMilliseconds / Convert.ToDouble(Environment.ProcessorCount);

        // Update the stored values for the next calculation
        lastCpuTimes[process.Id] = currentCpuTime;
        lastCheckTimes[process.Id] = currentTime;

        return cpuUsage;
    }

    private double GetGpuUsage(Process process)
    {
        double gpuUsage = 0.0;
        try
        {
            string query = $"SELECT UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine WHERE Name = {process.ProcessName}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                double gpuTime = Convert.ToDouble(obj["UtilizationPercentage"]);
                gpuUsage += gpuTime;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error querying WMI: " + ex.Message);
        }

        return gpuUsage;
    }

    private double GetNetworkDownload(Process process)
    {
        // Implement a method to get the network download speed for the process
        return 4.4; // Example value in KB/s
    }

    private double GetNetworkUpload(Process process)
    {
        // Implement a method to get the network upload speed for the process
        return 36; // Example value in KB/s
    }

    public void Dispose()
    {
        if (cpuCounters.Count > 0 && cpuCounters.Values.Count > 0)
            foreach (var counter in cpuCounters.Values)
                counter.Dispose();

        this.Dispose();
    }
}
#endif
