using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace DocxTemplate.UI.Converters;

/// <summary>
/// Converts boolean values to Thickness values for border styling
/// </summary>
public class BooleanToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return new Thickness(2);
        }
        return new Thickness(1);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}