using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Spectrometer.Models;
using System.Collections.ObjectModel;

namespace Spectrometer.Services
{
    public class ProcessesService
    {
        private Dictionary<int, PerformanceCounter> cpuCounters;
        private static readonly Lazy<ProcessesService> _instance = new(() => new ProcessesService());
        public static ProcessesService Instance => _instance.Value;

        public ProcessesService() 
        {
            cpuCounters = new Dictionary<int, PerformanceCounter>();
        }

        public ObservableCollection<ProcessInfo> LoadProcesses()
        {
            ObservableCollection<ProcessInfo> collection = new ObservableCollection<ProcessInfo>();
            var processes = Process.GetProcesses().Select(p => new ProcessInfo
            {
                ProcessName = p.ProcessName,
                CpuUsage = GetCpuUsage(p),
                GpuUsage = GetGpuUsage(p),
                MemoryUsage = p.WorkingSet64 / 1024.0 / 1024.0,
                DownloadUsage = GetNetworkDownload(p),
                UploadUsage = GetNetworkUpload(p)
            }).ToList();

            foreach(var process in processes)
            {
                collection.Add(process);
            }

            return collection;
        }

        private double GetCpuUsage(Process process)
        {
            // Implement a method to get the GPU usage for the process
            return 0.42; // Example value
        }

        private double GetGpuUsage(Process process)
        {
            // Implement a method to get the GPU usage for the process
            return 0.42; // Example value
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
    }
}
