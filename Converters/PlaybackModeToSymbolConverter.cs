using ObsMusicPlayer.Models;
using System.Globalization;
using System.Windows.Data;

namespace ObsMusicPlayer.Converters
{
    public class PlaybackModeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is PlaybackMode mode
                ? mode switch
                {
                    PlaybackMode.Normal => "➡",
                    PlaybackMode.RepeatAll => "🔁",
                    PlaybackMode.RepeatOne => "🔂",
                    PlaybackMode.Shuffle => "🔀",
                    _ => "➡"
                }
                : "➡";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
