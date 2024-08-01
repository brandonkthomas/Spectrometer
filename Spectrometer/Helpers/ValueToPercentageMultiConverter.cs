using System.Globalization;
using System.Windows.Data;

namespace Spectrometer.Helpers;

public class ValueToPercentageMultiConverter : IMultiValueConverter
{
    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 &&
            values[0] is IConvertible convertibleValue &&
            values[1] is IConvertible convertibleMaxValue)
        {
            double value = convertibleValue.ToDouble(CultureInfo.InvariantCulture);
            double maxValue = convertibleMaxValue.ToDouble(CultureInfo.InvariantCulture);

            if (maxValue != 0)
            {
                return (value / maxValue) * 100;
            }
        }

        return 0.0;
    }

    // -------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetTypes"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
