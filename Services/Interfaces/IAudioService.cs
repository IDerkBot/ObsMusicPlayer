namespace ObsMusicPlayer.Services.Interfaces
{
    public interface IAudioService : IDisposable
    {
        void Play(string filePath);
        void Pause();
        void Resume();
        void Stop();

        /// <summary>Текущая позиция в секундах</summary>
        double PositionSeconds { get; set; }

        /// <summary>Общая длительность в секундах (0 если ничего не загружено)</summary>
        double TotalSeconds { get; }

        /// <summary>Громкость 0.0 – 1.0</summary>
        float Volume { get; set; }

        bool IsPlaying { get; }
    }
}
