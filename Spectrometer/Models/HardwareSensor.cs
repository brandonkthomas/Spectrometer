using LibreHardwareMonitor.Hardware;
using System.ComponentModel;

namespace Spectrometer.Models;

// -------------------------------------------------------------------------------------------
// Modified Sensor Data Object

/// <summary>
/// Modified data object for hardware sensor data
/// Inherits LibreHardwareMonitor.Hardware.ISensor
/// Used in collections populated by HardwareMonitorService
/// </summary>
public partial class HardwareSensor(ISensor sensor) : ObservableObject, ISensor
{
    // -------------------------------------------------------------------------------------------
    // ISensor Properties

    public IControl Control { get; } = sensor.Control;
    public IHardware Hardware { get; } = sensor.Hardware;
    public Identifier Identifier { get; set; } = sensor.Identifier;
    public int Index { get; } = sensor.Index;
    public bool IsDefaultHidden { get; } = sensor.IsDefaultHidden;
    public float? Max { get; set; } = sensor.Max;
    public float? Min { get; set; } = sensor.Min;
    public string Name { get; set; } = sensor.Name;
    public IReadOnlyList<IParameter> Parameters { get; } = sensor.Parameters;
    public SensorType SensorType { get; } = sensor.SensorType;
    public float? Value { get; set; } = sensor.Value;
    public IEnumerable<SensorValue> Values { get; } = sensor.Values;
    public TimeSpan ValuesTimeWindow { get; set; } = sensor.ValuesTimeWindow;

    public void ResetMin() => Min = null;
    public void ResetMax() => Max = null;
    public void ClearValues() => (Values as IList<SensorValue>)?.Clear();

    public void Accept(IVisitor visitor) => visitor.VisitSensor(this);
    public void Traverse(IVisitor visitor) => visitor.VisitSensor(this);

    // -------------------------------------------------------------------------------------------
    // Computed Properties

    public override int GetHashCode() => HashCode.Combine(Identifier, Name);

    /// <summary>
    /// Allows for distinguishing multiple hardware items of the same type (i.e. 3 SSDs)
    /// </summary>
    public string GroupKey
    {
        get
        {
            var parts = Identifier.ToString().Split('/');
            if (parts.Length >= 3)
                return $"/{parts[1]}/{parts[2]}"; // nvme/0, nvme/1...

            return "Unknown";
        }
    }

    // -------------------------------------------------------------------------------------------
    // App-specific custom properties

    public bool IsPinned { get; set; }
    public bool IsGraphEnabled { get; set; }
}
