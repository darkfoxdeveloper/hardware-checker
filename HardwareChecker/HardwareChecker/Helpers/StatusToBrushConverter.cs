using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HardwareChecker.Models;

namespace HardwareChecker.Helpers;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DiagnosticStatus.Ok => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            DiagnosticStatus.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            DiagnosticStatus.Critical => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
