using LibreHardwareMonitor.Hardware;
using Spectrometer.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Spectrometer.Extensions;

public static class ObservableCollectionExtensions
{
    public static ObservableCollection<HardwareSensor> UpdateSensorCollection(
        this ObservableCollection<HardwareSensor> collection,
        IEnumerable<ISensor> newSensors)
    {
        // Remember existing sensors
        var existingSensors = collection.ToDictionary(sensor => sensor.Identifier);

        // Update existing sensors
        foreach (var newSensor in newSensors)
        {
            if (existingSensors.TryGetValue(newSensor.Identifier, out var existingSensor))
            {
                // Update existing sensor values while preserving custom properties
                existingSensor.Value = newSensor.Value;
                existingSensor.Min = newSensor.Min;
                existingSensor.Max = newSensor.Max;
                existingSensor.Name = newSensor.Name;
            }
        }

        return collection;
    }
}
