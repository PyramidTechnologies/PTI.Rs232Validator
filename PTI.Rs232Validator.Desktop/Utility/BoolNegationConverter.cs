using System;
using System.Globalization;
using System.Windows.Data;

namespace PTI.Rs232Validator.Desktop.Utility;

/// <summary>
/// An instance of <see cref="IValueConverter"/> that negates a boolean value.
/// </summary>
public class BoolNegationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
        {
            return false;
        }

        return !boolValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
        {
            return false;
        }

        return !boolValue;
    }
}