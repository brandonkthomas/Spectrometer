using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Spectrometer.Models;
using Spectrometer.Services;
using Spectrometer.ViewModels.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Spectrometer.ViewModels.UserControls;

public partial class GraphViewModel : ObservableObject
{
    // -------------------------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------------------------

    private readonly ObservableCollection<DateTimePoint> _values;
    private readonly MainWindowViewModel _mainWindowViewModel;

    public ObservableCollection<ISeries> Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public object Sync { get; } = new();
    public bool IsReading { get; set; } = true;
    public string ChartTitle { get; set; } = string.Empty;
    public HardwareSensor Sensor { get; set; }

    // -------------------------------------------------------------------------------------------
    // Constructor + Init
    //   I tried removing mainWindowViewMoel from the constructor, but it's needed for the
    //   HardwareSensor change handlers - they dont fire from the HardwareSensor class for some reason.
    //   TBD at a later date. For now it's fine.
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    /// <param name="sensor"></param>
    public GraphViewModel(MainWindowViewModel mainWindowViewModel, HardwareSensor sensor)
    {
        _values = [];
        _mainWindowViewModel = mainWindowViewModel;
        Sensor = sensor;

        // -------------------------------------------------------------------------------------------
        // Gradients

        LinearGradientPaint gradientFill = new(
            new SKColor(0, 151, 38, 200),
            new SKColor(0, 181, 157, 200));

        LinearGradientPaint gradientStroke = new(
            new SKColor(0, 151, 38),
            new SKColor(0, 181, 157));

        // -------------------------------------------------------------------------------------------
        // Graph Configuration

        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Values = _values,
                Fill = gradientFill,
                Stroke = gradientStroke,
                GeometryFill = null,
                GeometryStroke = null,
                Name = Sensor.Name,
            }
        ];

        XAxes = 
        [ 
            new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
            {
                CustomSeparators = [
                    DateTime.Now.AddSeconds(-25).Ticks,
                    DateTime.Now.AddSeconds(-20).Ticks,
                    DateTime.Now.AddSeconds(-15).Ticks,
                    DateTime.Now.AddSeconds(-10).Ticks,
                    DateTime.Now.AddSeconds(-5).Ticks,
                    DateTime.Now.Ticks
                ],
                ShowSeparatorLines = false,
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                LabelsPaint = new SolidColorPaint(SKColors.Transparent) // Hide bottom labels
            }
        ];

        YAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100,
                LabelsPaint = new SolidColorPaint(SKColors.White.WithAlpha(50))
            }
        ];

        ChartTitle = Sensor.Name;

        // -------------------------------------------------------------------------------------------
        // Bind PropertyChanged event to HwStatus list

        StartReadingData();
    }

    // -------------------------------------------------------------------------------------------
    // Chart
    // -------------------------------------------------------------------------------------------

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    private void StartReadingData()
    {
        if (_mainWindowViewModel?.HwMonSvc?.AllSensors != null) // HwMonSvc and AllSensors are ObservableObjects
            _mainWindowViewModel.HwMonSvc.PropertyChanged += HwMonSvc_PropertyChanged;
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// Update chart(s) on HwMonSvc property change
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HwMonSvc_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || !e.PropertyName.Contains("Sensors"))
            return;

        ObservableCollection<HardwareSensor>? sensors = e.PropertyName switch
        {
            nameof(HardwareMonitorService.AllSensors) => _mainWindowViewModel.HwMonSvc?.AllSensors,
            nameof(HardwareMonitorService.PinnedSensors) => _mainWindowViewModel.HwMonSvc?.PinnedSensors,
            nameof(HardwareMonitorService.MbSensors) => _mainWindowViewModel.HwMonSvc?.MbSensors,
            nameof(HardwareMonitorService.CpuSensors) => _mainWindowViewModel.HwMonSvc?.CpuSensors,
            nameof(HardwareMonitorService.GpuSensors) => _mainWindowViewModel.HwMonSvc?.GpuSensors,
            nameof(HardwareMonitorService.MemorySensors) => _mainWindowViewModel.HwMonSvc?.MemorySensors,
            nameof(HardwareMonitorService.StorageSensors) => _mainWindowViewModel.HwMonSvc?.StorageSensors,
            nameof(HardwareMonitorService.NetworkSensors) => _mainWindowViewModel.HwMonSvc?.NetworkSensors,
            nameof(HardwareMonitorService.ControllerSensors) => _mainWindowViewModel.HwMonSvc?.ControllerSensors,
            nameof(HardwareMonitorService.PsuSensors) => _mainWindowViewModel.HwMonSvc?.PsuSensors,
            _ => null,
        };

        if (sensors != null)
        {
            HardwareSensor? sensor = sensors.FirstOrDefault(s => s.Name == Sensor.Name);
            if (sensor != null)
            {
                UpdateChart(sensor.Value ?? float.NaN);

                Sensor = sensor;
                OnPropertyChanged(nameof(Sensor)); // update sensor value textblock in UI

                Logger.Write($"{sensor.Name},{sensor.Value}");
            }
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    private void UpdateChart(float value)
    {
        lock (Sync)
        {
            _values.Add(new DateTimePoint(DateTime.Now, value));
            if (_values.Count > 250) _values.RemoveAt(0);
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    private static string Formatter(DateTime date)
    {
        double secsAgo = (DateTime.Now - date).TotalSeconds;
        return secsAgo < 1 ? "now" : $"{secsAgo:N0}s ago";
    }
}
