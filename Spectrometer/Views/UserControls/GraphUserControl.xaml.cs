using Spectrometer.ViewModels.UserControls;
using System.Windows.Controls;
using System.Windows.Input;

namespace Spectrometer.Views.UserControls;

public partial class GraphUserControl : UserControl
{
    // -------------------------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewModel"></param>
    public GraphUserControl(GraphViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        this.MouseMove += GraphUserControl_MouseMove;
        this.MouseLeftButtonDown += GraphUserControl_MouseLeftButtonDown;
    }

    // -------------------------------------------------------------------------------------------
    // Drag/Drop Support
    // -------------------------------------------------------------------------------------------

    private Point _startPoint;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GraphUserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GraphUserControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DataObject dragData = new DataObject(typeof(GraphUserControl), this);
                DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
            }
        }
    }
}
