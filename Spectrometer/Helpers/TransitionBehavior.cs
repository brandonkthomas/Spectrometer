using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Wpf.Ui.Animations;

namespace Spectrometer.Helpers;

public static class TransitionBehavior
{
    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    public static readonly DependencyProperty TransitionProperty = DependencyProperty.RegisterAttached(
        "Transition",
        typeof(Transition),
        typeof(TransitionBehavior),
        new PropertyMetadata(Transition.None, OnTransitionChanged));

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static Transition GetTransition(DependencyObject obj) => (Transition)obj.GetValue(TransitionProperty);

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="value"></param>
    public static void SetTransition(DependencyObject obj, Transition value) => obj.SetValue(TransitionProperty, value);

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="d"></param>
    /// <param name="e"></param>
    private static void OnTransitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.IsVisibleChanged += (sender, eventArgs) =>
            {
                if ((bool)eventArgs.NewValue)
                {
                    ApplyTransition(element, (Transition)e.NewValue, true);
                }
                else
                {
                    ApplyTransition(element, (Transition)e.NewValue, false);
                }
            };
        }
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="element"></param>
    /// <param name="transition"></param>
    /// <param name="isVisible"></param>
    private static void ApplyTransition(FrameworkElement element, Transition transition, bool isVisible)
    {
        var storyboard = new Storyboard();

        switch (transition)
        {
            case Transition.FadeIn:
                AddFadeAnimation(storyboard, isVisible);
                break;
            case Transition.FadeInWithSlide:
                AddFadeAnimation(storyboard, isVisible);
                AddSlideAnimation(storyboard, isVisible, fromBottom: true);
                break;
            case Transition.SlideBottom:
                AddSlideAnimation(storyboard, isVisible, fromBottom: true);
                break;
            case Transition.SlideLeft:
                AddSlideAnimation(storyboard, isVisible, fromLeft: true);
                break;
            case Transition.SlideRight:
                AddSlideAnimation(storyboard, isVisible, fromRight: true);
                break;
        }

        storyboard.Begin(element);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="storyboard"></param>
    /// <param name="isVisible"></param>
    private static void AddFadeAnimation(Storyboard storyboard, bool isVisible)
    {
        var animation = new DoubleAnimation
        {
            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
            From = isVisible ? 0 : 1,
            To = isVisible ? 1 : 0
        };

        Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
        storyboard.Children.Add(animation);
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="storyboard"></param>
    /// <param name="isVisible"></param>
    /// <param name="fromBottom"></param>
    /// <param name="fromLeft"></param>
    /// <param name="fromRight"></param>
    private static void AddSlideAnimation(Storyboard storyboard, bool isVisible, bool fromBottom = false, bool fromLeft = false, bool fromRight = false)
    {
        var animation = new DoubleAnimation
        {
            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
            From = isVisible ? (fromBottom ? 50 : (fromLeft ? -50 : (fromRight ? 50 : 0))) : 0,
            To = isVisible ? 0 : (fromBottom ? 50 : (fromLeft ? -50 : (fromRight ? 50 : 0))),
        };

        if (fromBottom || fromLeft || fromRight)
        {
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        }
        else
        {
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        }

        storyboard.Children.Add(animation);
    }
}
