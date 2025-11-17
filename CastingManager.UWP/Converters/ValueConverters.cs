using System;
using Windows.Media.Casting;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace CastingManager.UWP.Converters
{
    /// <summary>
    /// Converts CastingConnectionState to a color brush for status indicators
    /// </summary>
    public partial class ConnectionStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CastingConnectionState state)
            {
                return state switch
                {
                    CastingConnectionState.Connected => new SolidColorBrush(Color.FromArgb(255, 16, 124, 16)), // Green
                    CastingConnectionState.Connecting => new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)), // Orange
                    CastingConnectionState.Disconnected => new SolidColorBrush(Color.FromArgb(255, 118, 118, 118)), // Gray
                    _ => new SolidColorBrush(Color.FromArgb(255, 118, 118, 118))
                };
            }
            return new SolidColorBrush(Color.FromArgb(255, 118, 118, 118));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean scanning state to color
    /// </summary>
    public partial class ScanningStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isScanning)
            {
                return isScanning 
                    ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)) // Blue
                    : new SolidColorBrush(Color.FromArgb(255, 118, 118, 118)); // Gray
            }
            return new SolidColorBrush(Color.FromArgb(255, 118, 118, 118));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean scanning state to text
    /// </summary>
    public partial class ScanningStateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isScanning)
            {
                return isScanning ? "Scanning" : "Idle";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean initialized state to color
    /// </summary>
    public partial class InitializedStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isInitialized)
            {
                return isInitialized 
                    ? new SolidColorBrush(Color.FromArgb(255, 16, 124, 16)) // Green
                    : new SolidColorBrush(Color.FromArgb(255, 196, 43, 28)); // Red
            }
            return new SolidColorBrush(Color.FromArgb(255, 118, 118, 118));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean initialized state to text
    /// </summary>
    public partial class InitializedStateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isInitialized)
            {
                return isInitialized ? "Ready" : "Not Ready";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to Visibility
    /// </summary>
    public partial class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts boolean to inverse Visibility
    /// </summary>
    public partial class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }

    /// <summary>
    /// Converts count to Visibility (Visible when count is 0)
    /// </summary>
    public partial class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts count to inverse Visibility (Visible when count > 0)
    /// </summary>
    public partial class InverseCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts signal strength (0-1) to percentage string
    /// </summary>
    public partial class SignalStrengthToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double strength)
            {
                return $"{strength:P0}";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts retry attempts to display text
    /// </summary>
    public partial class RetryAttemptsToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int attempts)
            {
                return attempts > 0 ? $"(Attempt {attempts})" : "";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts DateTime to relative time string
    /// </summary>
    public partial class RelativeTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                var timeSpan = DateTime.Now - dateTime;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{timeSpan.Minutes} min ago";
                if (timeSpan.TotalHours < 24)
                    return $"{timeSpan.Hours} hr ago";
                
                return dateTime.ToString("MMM dd, HH:mm");
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts string array to comma-separated string
    /// </summary>
    public partial class StringArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string[] array)
            {
                return string.Join(", ", array);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}