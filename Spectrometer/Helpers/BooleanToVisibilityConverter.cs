using System.Globalization;
using System.Windows.Data;

namespace Spectrometer.Helpers;

public class BooleanToVisibilityConverter : IValueConverter
{
    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            bool invert = parameter != null && System.Convert.ToBoolean(parameter);
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool invert = parameter != null && System.Convert.ToBoolean(parameter);
            return (visibility == Visibility.Visible) ^ invert;
        }

        return false;
    }
}
