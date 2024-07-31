using LibreHardwareMonitor.Hardware;
using Spectrometer.Models;
using System.Globalization;
using System.Windows.Data;

namespace Spectrometer.Helpers;

public class ValueWithSuffixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HardwareSensor sensorData)
        {
            return sensorData.SensorType switch
            {
                SensorType.Clock => $"{sensorData.Value:F0} MHz",
                SensorType.Control => $"{sensorData.Value:F1}%",
                SensorType.Current => $"{sensorData.Value:F1} A",
                SensorType.Data => $"{sensorData.Value:F1} GB",
                SensorType.Energy => $"{sensorData.Value:F1} energy",
                SensorType.Factor => $"{sensorData.Value:F1} factor",
                SensorType.Fan => $"{sensorData.Value:F0} RPM",
                SensorType.Flow => $"{sensorData.Value:F1} L/h",
                SensorType.Frequency => $"{sensorData.Value:F1} Hz",
                SensorType.Level => $"{sensorData.Value:F1}%",
                SensorType.Load => $"{sensorData.Value:F1}%",
                SensorType.Noise => $"{sensorData.Value:F1} dB",
                SensorType.Power => $"{sensorData.Value:F1} W",
                SensorType.SmallData => $"{sensorData.Value:F0} MB",
                SensorType.Temperature => $"{sensorData.Value:F1}°C",
                SensorType.Throughput => $"{sensorData.Value:F0} MB/s",
                SensorType.TimeSpan => $"{sensorData.Value:F1} s",
                SensorType.Voltage => $"{sensorData.Value:F1} W",
                _ => sensorData.Value
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
