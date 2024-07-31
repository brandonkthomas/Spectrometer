using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Spectrometer.Models;
using Spectrometer.ViewModels.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Spectrometer.ViewModels.UserControls;

public partial class GraphViewModel : ObservableObject
{
    // ------------------------------------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------------------------------------

    private readonly ObservableCollection<DateTimePoint> _values;
    private readonly DateTimeAxis _customAxis;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly HardwareSensor _sensor;

    public ObservableCollection<ISeries> Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public object Sync { get; } = new object();
    public bool IsReading { get; set; } = true;
    public string ChartTitle { get; set; } = string.Empty;

    // ------------------------------------------------------------------------------------------------
    // Constructor + Init
    //   I tried removing mainWindowViewMoel from the constructor, but it's needed for the
    //   HardwareSensor change handlers - they dont fire from the HardwareSensor class for some reason.
    //   TBD at a later date. For now it's fine.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    /// <param name="sensor"></param>
    public GraphViewModel(MainWindowViewModel mainWindowViewModel, HardwareSensor sensor)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _sensor = sensor;
        _values = new ObservableCollection<DateTimePoint>();

        // ------------------------------------------------------------------------------------------------
        // Gradients

        var gradientFill = new LinearGradientPaint(
            new SKColor(0, 151, 38, 90),
            new SKColor(0, 181, 157, 90));

        var gradientStroke = new LinearGradientPaint(
            new SKColor(0, 151, 38, 0),
            new SKColor(0, 181, 157, 0));

        // ------------------------------------------------------------------------------------------------
        // Graph Configuration

        Series = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = _values,
                Fill = gradientFill,
                Stroke = gradientStroke,
                GeometryFill = null,
                GeometryStroke = null,
                Name = _sensor.Name,
            }
        };

        _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
        {
            ShowSeparatorLines = false,
            AnimationsSpeed = TimeSpan.FromMilliseconds(0),
            SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100)),
            LabelsPaint = new SolidColorPaint(SKColors.Transparent) // Hide bottom labels
        };

        XAxes = [ _customAxis ];
        YAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100,
                LabelsPaint = new SolidColorPaint(SKColors.White.WithAlpha(50))
            }
        ];

        ChartTitle = _sensor.Name;

        StartReadingData();
    }

    // ------------------------------------------------------------------------------------------------
    // Chart
    // ------------------------------------------------------------------------------------------------

    private void StartReadingData()
    {
        if (_mainWindowViewModel?.HwStatus != null)
        {
            _mainWindowViewModel.HwStatus.PropertyChanged += HwStatus_PropertyChanged;
            foreach (var sensor in _mainWindowViewModel.HwStatus.CpuSensors)
            {
                if (sensor.Name == _sensor.Name)
                {
                    sensor.PropertyChanged += Sensor_PropertyChanged;
                    break;
                }
            }
        }
    }

    private void HwStatus_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || !e.PropertyName.Contains("Sensors"))
            return;

        ObservableCollection<HardwareSensor>? sensors = e.PropertyName switch
        {
            nameof(HardwareStatus.CpuSensors) => _mainWindowViewModel.HwStatus?.CpuSensors,
            nameof(HardwareStatus.GpuSensors) => _mainWindowViewModel.HwStatus?.GpuSensors,
            nameof(HardwareStatus.MbSensors) => _mainWindowViewModel.HwStatus?.MbSensors,
            nameof(HardwareStatus.MemorySensors) => _mainWindowViewModel.HwStatus?.MemorySensors,
            nameof(HardwareStatus.StorageSensors) => _mainWindowViewModel.HwStatus?.StorageSensors,
            _ => null,
        };

        if (sensors != null)
        {
            var sensor = sensors.FirstOrDefault(s => s.Name == _sensor.Name);
            if (sensor != null)
            {
                sensor.PropertyChanged += Sensor_PropertyChanged;
                UpdateChart(sensor.Value);
            }
        }
    }

    private void Sensor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HardwareSensor.Value))
        {
            var sensor = sender as HardwareSensor;
            if (sensor != null)
            {
                UpdateChart(sensor.Value);
            }
        }
    }

    private void UpdateChart(float cpuUsage)
    {
        lock (Sync)
        {
            _values.Add(new DateTimePoint(DateTime.Now, cpuUsage));
            if (_values.Count > 250) _values.RemoveAt(0);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    private static string Formatter(DateTime date)
    {
        var secsAgo = (DateTime.Now - date).TotalSeconds;

        return secsAgo < 1
            ? "now"
            : $"{secsAgo:N0}s ago";
    }
}
