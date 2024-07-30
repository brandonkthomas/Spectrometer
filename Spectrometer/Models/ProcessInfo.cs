using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spectrometer.Models
{
    public partial class ProcessInfo : ObservableObject
    {
        [ObservableProperty]
        private string _processName = "";

        [ObservableProperty]
        private double _cpuUsage = 0;

        [ObservableProperty]
        private double _gpuUsage = 0;

        [ObservableProperty]
        private double _memoryUsage = 0;

        [ObservableProperty]
        private double _downloadUsage = 0;

        [ObservableProperty]
        private double _uploadUsage = 0;
    }
}
