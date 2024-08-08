using Spectrometer.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;

namespace Spectrometer.Helpers;

public class StringToThemedImageSourceConverter : IValueConverter
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
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string imageNameNoSuffix && !string.IsNullOrEmpty(imageNameNoSuffix))
        {
            try
            {
                string themeSuffix = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? "-white" : "-black";
                string themedPath = $"{imageNameNoSuffix}{themeSuffix}.png";

                // Create a Uri and return a BitmapImage
                Uri imageUri = new(themedPath, UriKind.RelativeOrAbsolute);
                return new BitmapImage(imageUri);
            }
            catch (Exception ex)
            {
                Logger.WriteExc(ex);
                return null;
            }
        }
        return null;
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
