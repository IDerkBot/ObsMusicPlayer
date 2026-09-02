namespace ObsMusicPlayer.Models
{
    public class NowPlayingInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public TimeSpan Position { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Для отображения
        public string DisplayText => $"{Artist} - {Title}";
        public string ProgressText => $"{Position:mm\\:ss} / {Duration:mm\\:ss}";
    }
}
