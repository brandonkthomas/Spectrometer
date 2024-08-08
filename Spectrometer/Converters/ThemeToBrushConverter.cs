using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace Spectrometer.Helpers;

internal class ThemeToBrushConverter : IValueConverter
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
        var currentTheme = ApplicationThemeManager.GetAppTheme();

        return currentTheme == ApplicationTheme.Dark ?
            Application.Current.Resources["ApplicationBackgroundColorDarkBrush"] :
            Application.Current.Resources["ApplicationBackgroundColorLightBrush"];
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
    /// <exception cref="NotImplementedException"></exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
