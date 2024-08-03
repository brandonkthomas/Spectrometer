using LibreHardwareMonitor.Hardware;
using Spectrometer.Models;
using System.Globalization;
using System.Windows.Data;

namespace Spectrometer.Helpers;

public class SensorTypeToFormattedValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ISensor sensorData && sensorData.Value is not null)
            return _formatSensorValue(sensorData.SensorType, sensorData.Value.Value);

        if (value is HardwareSensor hwSensorData && hwSensorData.Value is not null)
            return _formatSensorValue(hwSensorData.SensorType, hwSensorData.Value.Value);

        return string.Empty;
    }

    private string _formatSensorValue(SensorType sensorType, float sensorValue)
    {
        return sensorType switch
        {
            SensorType.Clock => $"{sensorValue:F0} MHz",
            SensorType.Control => $"{sensorValue:F1}%",
            SensorType.Current => $"{sensorValue:F1} A",
            SensorType.Data => $"{sensorValue:F1} GB",
            SensorType.Energy => $"{sensorValue:F1} energy",
            SensorType.Factor => $"{sensorValue:F1} factor",
            SensorType.Fan => $"{sensorValue:F0} RPM",
            SensorType.Flow => $"{sensorValue:F1} L/h",
            SensorType.Frequency => $"{sensorValue:F1} Hz",
            SensorType.Level => $"{sensorValue:F1}%",
            SensorType.Load => $"{sensorValue:F1}%",
            SensorType.Noise => $"{sensorValue:F1} dB",
            SensorType.Power => $"{sensorValue:F1} W",
            SensorType.SmallData => $"{sensorValue:F0} MB",
            SensorType.Temperature => $"{sensorValue:F1}°C",
            SensorType.Throughput => $"{sensorValue:F0} MB/s",
            SensorType.TimeSpan => $"{sensorValue:F1} s",
            SensorType.Voltage => $"{sensorValue:F1} V", // Changed to V to be more accurate for voltage
            _ => sensorValue.ToString(CultureInfo.InvariantCulture)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
