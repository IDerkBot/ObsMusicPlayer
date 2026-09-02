using ObsMusicPlayer.Models;

namespace ObsMusicPlayer.Services.Interfaces
{
    public interface INowPlayingService : IDisposable
    {
        /// <summary>Обновить информацию о текущем треке</summary>
        void UpdateNowPlaying(NowPlayingInfo info);

        /// <summary>Очистить (трек остановлен)</summary>
        void Clear();

        /// <summary>Событие для overlay окна</summary>
        event Action<NowPlayingInfo?> NowPlayingChanged;
    }
}
