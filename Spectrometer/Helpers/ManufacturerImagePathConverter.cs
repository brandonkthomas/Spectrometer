using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace Spectrometer.Helpers;

public class ManufacturerImagePathConverter : IValueConverter
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
        if (value == null || parameter == null)
            return string.Empty;

        string name = value.ToString() ?? "";
        string[] keywords = parameter.ToString()?.Split(',') ?? [];

        var isDarkMode = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        string logoColor = isDarkMode ? "white" : "black";

        foreach (var keyword in keywords)
        {
            if (name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                return $"pack://application:,,,/Assets/Manufacturers/{keyword}-logo-{logoColor}.png";
        }

        return string.Empty;
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
