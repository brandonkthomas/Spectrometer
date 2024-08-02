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
public partial class HardwareSensor : ObservableObject, ISensor
{
    // -------------------------------------------------------------------------------------------
    // ISensor => HardwareSensor constructor

    public HardwareSensor(ISensor sensor)
    {
        Control = sensor.Control;
        Hardware = sensor.Hardware;
        Identifier = sensor.Identifier;
        Index = sensor.Index;
        IsDefaultHidden = sensor.IsDefaultHidden;
        Max = sensor.Max;
        Min = sensor.Min;
        Name = sensor.Name;
        Parameters = sensor.Parameters;
        SensorType = sensor.SensorType;
        Value = sensor.Value;
        Values = sensor.Values;
        ValuesTimeWindow = sensor.ValuesTimeWindow;
    }

    // -------------------------------------------------------------------------------------------
    // ISensor Properties

    public IControl Control { get; }
    public IHardware Hardware { get; }
    public Identifier Identifier { get; set; }
    public int Index { get; }
    public bool IsDefaultHidden { get; }
    public float? Max { get; set; }
    public float? Min { get; set; }
    public string Name { get; set; }
    public IReadOnlyList<IParameter> Parameters { get; }
    public SensorType SensorType { get; }
    public float? Value { get; set; }
    public IEnumerable<SensorValue> Values { get; }
    public TimeSpan ValuesTimeWindow { get; set; }

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
