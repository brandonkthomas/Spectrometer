using LibreHardwareMonitor.Hardware;
using Spectrometer.Models;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace Spectrometer.Helpers;

public class SensorTypeToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ISensor sensorData)
            return _getIconForSensorType(sensorData.SensorType);

        if (value is HardwareSensor hwSensorData)
            return _getIconForSensorType(hwSensorData.SensorType);

        return SymbolRegular.Empty;
    }

    private object _getIconForSensorType(SensorType sensorType)
    {
        return sensorType switch
        {
            SensorType.Clock => SymbolRegular.Clock20,
            SensorType.Control => SymbolRegular.DataTrending20,
            SensorType.Current => SymbolRegular.Power20,
            SensorType.Data => SymbolRegular.Storage20,
            SensorType.Energy => SymbolRegular.Power20,
            SensorType.Factor => SymbolRegular.TextAsterisk20,
            SensorType.Fan => SymbolRegular.FastAcceleration20,
            SensorType.Flow => SymbolRegular.Flow20,
            SensorType.Frequency => SymbolRegular.ArrowClockwise20,
            SensorType.Level => SymbolRegular.DataTrending20,
            SensorType.Load => SymbolRegular.DataTrending20,
            SensorType.Noise => SymbolRegular.Speaker220,
            SensorType.Power => SymbolRegular.Power20,
            SensorType.SmallData => SymbolRegular.Storage20,
            SensorType.Temperature => SymbolRegular.Temperature20,
            SensorType.Throughput => SymbolRegular.TopSpeed20,
            SensorType.TimeSpan => SymbolRegular.Clock20,
            SensorType.Voltage => SymbolRegular.DatabaseLightning20,
            _ => SymbolRegular.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
