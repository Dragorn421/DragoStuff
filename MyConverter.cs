using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

namespace avalonia_contextmenu_xaml;

public class MyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.Assert(value is string);
        Debug.Assert(parameter is string);
        return (string)parameter + (string)value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
