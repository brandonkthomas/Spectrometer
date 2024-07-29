using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Spectrometer.Helpers;

public class StringToImageSourceConverter : IValueConverter
{
    // ------------------------------------------------------------------------------------------------
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
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                Uri imageUri = new Uri(path, UriKind.RelativeOrAbsolute);
                return new BitmapImage(imageUri);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    // ------------------------------------------------------------------------------------------------
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
