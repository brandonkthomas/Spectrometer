﻿using Spectrometer.Models;
using Spectrometer.ViewModels.Pages;
using Spectrometer.ViewModels.UserControls;
using Spectrometer.ViewModels.Windows;
using Spectrometer.Views.UserControls;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Spectrometer.Views.Pages;

public partial class GraphsPage : INavigableView<GraphsViewModel>, INavigationAware
{
    // -------------------------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------------------------

    public GraphsViewModel ViewModel { get; }

    public bool IsLoading { get; set; }

    private readonly MainWindowViewModel _mainWindowViewModel;

    // -------------------------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------------------------

    public GraphsPage(GraphsViewModel viewModel, MainWindowViewModel mainWindowViewModel)
    {
        Logger.Write("GraphsPage initializing...");
        IsLoading = true;

        ViewModel = viewModel;
        DataContext = viewModel;
        _mainWindowViewModel = mainWindowViewModel;

        InitializeComponent();

        ClearControls();
        LoadGraphControls();

        IsLoading = false;
        Logger.Write("GraphsPage initialized");
    }

    // -------------------------------------------------------------------------------------------
    // Graph Building
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    public void LoadGraphControls()
    {
        List<HardwareSensor> sensors = GetEnabledGraphSensors();

        if (sensors.Count == 0)
        {
            var textBlock = new Wpf.Ui.Controls.TextBlock
            {
                Text = "No sensors available for graphing.\nUse the Sensors tab to enable individual graphs.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16,
            };

            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetRow(textBlock, 0);
            Grid.SetColumn(textBlock, 0);

            ContentGrid.Children.Add(textBlock);
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
            var viewModel = new GraphViewModel(_mainWindowViewModel, sensor);
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

    /// <summary>
    /// Helper function: Concat saved sensors (settings) with global sensors where IsGraphEnabled = true
    /// </summary>
    /// <param name="mainWindowViewModel"></param>
    /// <returns></returns>
    private List<HardwareSensor> GetEnabledGraphSensors()
    {
        if (_mainWindowViewModel.HwMonSvc == null)
            return [];

        // -------------------------------------------------------------------------------------------
        // Check settings first

        List<string>? graphedSensorIdentifiers = App.SettingsMgr?.Settings?.GraphedSensorIdentifiers;
        List<HardwareSensor> graphedSensors = [];

        if (graphedSensorIdentifiers != null)
            graphedSensors = graphedSensorIdentifiers
                .Select(id => _mainWindowViewModel.HwMonSvc.AllSensors?.FirstOrDefault(s => s.Identifier.ToString() == id))
                .Where(s => s != null)
                .Cast<HardwareSensor>()
                .ToList();

        // -------------------------------------------------------------------------------------------
        // Concat saved sensors with currently enabled sensors

        List<HardwareSensor> graphedSensorsConcat = _mainWindowViewModel.HwMonSvc.AllSensors?
            .Where(s => s.IsGraphEnabled)
            .Concat(graphedSensors)
            .ToList() ?? [];

        Logger.Write($"{graphedSensorsConcat.Count} graphed sensor(s) found in settings & matched to available system sensors");
        return graphedSensorsConcat;
    }

    // -------------------------------------------------------------------------------------------
    // XAML Management
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    public void ClearControls()
    {
        if (ContentGrid == null)
            return;

        ContentGrid.Children.Clear();
        ContentGrid.RowDefinitions.Clear();
        ContentGrid.ColumnDefinitions.Clear();
        Logger.Write("GraphsPage controls cleared");
    }

    // -------------------------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Refresh controls on navigation change
    /// </summary>
    public void OnNavigatedTo()
    {
        ClearControls();
        LoadGraphControls();
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnNavigatedFrom() { }

    // -------------------------------------------------------------------------------------------
    // Graph Drag and Drop Support (WIP)
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(GraphUserControl)))
            e.Effects = DragDropEffects.Move;
        else
            e.Effects = DragDropEffects.None;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="child"></param>
    /// <returns></returns>
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
