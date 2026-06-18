using System.Globalization;
using System.Windows.Data;
using HardwareChecker.Models;

namespace HardwareChecker.Helpers;

public sealed class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DiagnosticStatus.Ok => "Correcto",
            DiagnosticStatus.Warning => "Advertencia",
            DiagnosticStatus.Critical => "Critico",
            _ => "Sin datos"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
