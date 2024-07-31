using Spectrometer.Models;
using Spectrometer.ViewModels.Pages;
using Spectrometer.ViewModels.UserControls;
using Spectrometer.ViewModels.Windows;
using Spectrometer.Views.UserControls;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class GraphsPage : INavigableView<GraphsViewModel>
{
    public GraphsViewModel ViewModel { get; }

    // ------------------------------------------------------------------------------------------------
    // Constructor

    public GraphsPage(GraphsViewModel viewModel, MainWindowViewModel mainWindowViewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
        LoadGraphControls(mainWindowViewModel);
    }

    // ------------------------------------------------------------------------------------------------
    // Graph Building

    private void LoadGraphControls(MainWindowViewModel mainWindowViewModel)
    {
        List<HardwareSensor> sensors = GetEnabledGraphSensors(mainWindowViewModel);

        // Clear existing controls (there shouldn't be any)
        ContentGrid.Children.Clear();
        ContentGrid.RowDefinitions.Clear();
        ContentGrid.ColumnDefinitions.Clear();

        if (sensors.Count == 0)
        {
            // Display a TextBox in the center of the grid
            var textBox = new Wpf.Ui.Controls.TextBox
            {
                Text = "No sensors available for graphing.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                FontSize = 16
            };

            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetRow(textBox, 1);
            Grid.SetColumn(textBox, 0);

            ContentGrid.Children.Add(textBox);
            return;
        }

        // Define grid layout
        int rows = (sensors.Count + 1) / 2;
        for (int i = 0; i < rows; i++)
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Add controls
        for (int i = 0; i < sensors.Count; i++)
        {
            var sensor = sensors[i];
            var viewModel = new GraphViewModel(mainWindowViewModel, sensor);
            var graphControl = new GraphUserControl(viewModel);

            var card = new CardAction
            {
                Content = graphControl,
                Height = 260,
                Width = 400,
                Margin = new Thickness(5),
                IsChevronVisible = false
            };

            var row = i / 2;
            var column = i % 2;
            Grid.SetRow(card, row);
            Grid.SetColumn(card, column);

            ContentGrid.Children.Add(card);
        }
    }

    private List<HardwareSensor> GetEnabledGraphSensors(MainWindowViewModel mainWindowViewModel)
    {
        if (mainWindowViewModel.HwStatus == null)
            return [];

        var test = mainWindowViewModel.HwStatus.CpuSensors.Where(s => s.Name.Contains("Package")).FirstOrDefault();
        if (test != null) test.IsGraphEnabled = true;

        return mainWindowViewModel.HwStatus.MbSensors.Where(s => s.IsGraphEnabled)
            .Concat(mainWindowViewModel.HwStatus.CpuSensors.Where(s => s.IsGraphEnabled))
            .Concat(mainWindowViewModel.HwStatus.GpuSensors.Where(s => s.IsGraphEnabled))
            .Concat(mainWindowViewModel.HwStatus.MemorySensors.Where(s => s.IsGraphEnabled))
            .Concat(mainWindowViewModel.HwStatus.StorageSensors.Where(s => s.IsGraphEnabled))
            .ToList();
    }

    // ------------------------------------------------------------------------------------------------
    // Graph Drag and Drop Support

    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(GraphUserControl)))
            e.Effects = DragDropEffects.Move;
        else
            e.Effects = DragDropEffects.None;
    }

    private void Grid_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(GraphUserControl)))
        {
            var droppedData = e.Data.GetData(typeof(GraphUserControl)) as GraphUserControl;
            var target = e.OriginalSource as UIElement;

            if (droppedData != null && target != null)
            {
                var targetCardAction = FindParent<CardAction>(target);
                var droppedCardAction = FindParent<CardAction>(droppedData);

                if (targetCardAction != null && droppedCardAction != null)
                {
                    var grid = (Grid)targetCardAction.Parent;
                    if (grid != null)
                    {
                        int targetIndex = grid.Children.IndexOf(targetCardAction);
                        int droppedIndex = grid.Children.IndexOf(droppedCardAction);

                        if (targetIndex != droppedIndex)
                        {
                            grid.Children.RemoveAt(droppedIndex);
                            grid.Children.Insert(targetIndex, droppedCardAction);
                        }
                    }
                }
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);

        if (parentObject == null)
            return null;

        T? parent = parentObject as T;
        if (parent != null)
            return parent;

        return FindParent<T>(parentObject);
    }
}
