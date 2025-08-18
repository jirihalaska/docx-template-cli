using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DocxTemplate.UI.Converters;

/// <summary>
/// Converts boolean values to Brush colors based on parameter specification
/// Parameter format: "TrueBrush|FalseBrush" (e.g., "Red|Green" or "Orange|Transparent")
/// </summary>
public class BooleanToBrushConverter : IValueConverter
{
    public static readonly BooleanToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string parameterString)
            return Brushes.Transparent;

        var parts = parameterString.Split('|');
        if (parts.Length != 2)
            return Brushes.Transparent;

        var trueBrushName = parts[0].Trim();
        var falseBrushName = parts[1].Trim();

        var selectedBrushName = boolValue ? trueBrushName : falseBrushName;

        return GetBrushByName(selectedBrushName);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static IBrush GetBrushByName(string brushName)
    {
        return brushName.ToLowerInvariant() switch
        {
            "transparent" => Brushes.Transparent,
            "red" => Brushes.Red,
            "orange" => Brushes.Orange,
            "yellow" => Brushes.Yellow,
            "green" => Brushes.Green,
            "blue" => Brushes.Blue,
            "purple" => Brushes.Purple,
            "black" => Brushes.Black,
            "white" => Brushes.White,
            "gray" => Brushes.Gray,
            "lightgray" => Brushes.LightGray,
            "darkgray" => Brushes.DarkGray,
            _ => Brushes.Transparent
        };
    }
}