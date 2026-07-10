using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CadEditor.Models;

namespace CadEditor.Views.Converters;

public class Point2DToPointConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Point2D p ? new Point(p.X, p.Y) : new Point();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}