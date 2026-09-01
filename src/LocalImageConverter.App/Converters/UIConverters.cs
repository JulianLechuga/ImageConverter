using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LocalImageConverter.Core.Models;

namespace LocalImageConverter.App.Converters;

public class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush WaitingBrush = new(Color.FromRgb(148, 163, 184));    // Slate 400
    private static readonly SolidColorBrush ProcessingBrush = new(Color.FromRgb(59, 130, 246));  // Blue 500
    private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(16, 185, 129));  // Emerald 500
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(239, 68, 68));       // Rose 500
    private static readonly SolidColorBrush CancelledBrush = new(Color.FromRgb(245, 158, 11));   // Amber 500

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ItemStatus status)
        {
            return status switch
            {
                ItemStatus.Processing => ProcessingBrush,
                ItemStatus.Completed => CompletedBrush,
                ItemStatus.Error => ErrorBrush,
                ItemStatus.Cancelled => CancelledBrush,
                ItemStatus.Waiting or _ => WaitingBrush
            };
        }

        return WaitingBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
        if (Invert) isNull = !isNull;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is true;
        if (Invert) isTrue = !isTrue;
        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
