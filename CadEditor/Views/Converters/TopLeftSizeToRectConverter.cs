using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CadEditor.Models;

namespace CadEditor.Views.Converters;

public class TopLeftSizeToRectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not Point2D topLeft
            || values[1] is not double width || values[2] is not double height)
            return Rect.Empty;

        return new Rect(topLeft.X, topLeft.Y, Math.Max(width, 0), Math.Max(height, 0));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}