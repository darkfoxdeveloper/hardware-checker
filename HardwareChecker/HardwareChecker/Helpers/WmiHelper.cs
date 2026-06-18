using System.Management;

namespace HardwareChecker.Helpers;

public static class WmiHelper
{
    public static List<ManagementObject> Query(string query, string scope = @"root\CIMV2")
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, query);
            return searcher.Get().Cast<ManagementObject>().ToList();
        }
        catch
        {
            return [];
        }
    }

    public static T? GetValue<T>(ManagementBaseObject? obj, string propertyName)
    {
        try
        {
            var value = obj?[propertyName];
            if (value is null)
            {
                return default;
            }

            if (value is T typed)
            {
                return typed;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
