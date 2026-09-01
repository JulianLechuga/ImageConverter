using System.Globalization;
using System.Windows.Data;

namespace LocalImageConverter.App.Converters;

public class ByteSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return "-";

        long bytes = 0;
        if (value is long l) bytes = l;
        else if (value is int i) bytes = i;
        else if (value is double d) bytes = (long)d;
        else if (long.TryParse(value.ToString(), out var parsed)) bytes = parsed;

        return FormatBytes(bytes);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double doubleBytes = bytes;

        while (doubleBytes >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            doubleBytes /= 1024.0;
            suffixIndex++;
        }

        return $"{doubleBytes:0.##} {suffixes[suffixIndex]}";
    }
}
