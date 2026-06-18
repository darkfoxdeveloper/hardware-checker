namespace HardwareChecker.Helpers;

public static class FormatHelper
{
    public static string Percent(double value) => $"{value:0}%";

    public static string Gb(double value) => $"{value:0.0} GB";

    public static double BytesToGb(double bytes) => bytes / 1024d / 1024d / 1024d;

    public static double KbToGb(ulong kb) => kb / 1024d / 1024d;

    public static string OptionalTemperature(double? value) =>
        value.HasValue ? $"{value.Value:0} C" : "No disponible";

    public static string OptionalMhz(double? value) =>
        value.HasValue ? $"{value.Value:0} MHz" : "No disponible";
}
