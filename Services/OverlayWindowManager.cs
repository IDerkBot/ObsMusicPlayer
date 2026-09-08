using ObsMusicPlayer.Models;
using ObsMusicPlayer.Services.Interfaces;
using ObsMusicPlayer.Views;
using System.Windows;

namespace ObsMusicPlayer.Services
{
    public class OverlayWindowManager : IDisposable
    {
        private readonly NowPlayingOverlayWindow _window;
        private readonly INowPlayingService _nowPlayingService;
        private bool _isDisposed;

        public OverlayWindowManager(INowPlayingService nowPlayingService)
        {
            _nowPlayingService = nowPlayingService;
            _window = new NowPlayingOverlayWindow();

            // Подписываемся на события
            _nowPlayingService.NowPlayingChanged += OnNowPlayingChanged;
        }

        private void OnNowPlayingChanged(NowPlayingInfo? info)
        {
            if (_isDisposed) return;

            return;

            // Важно: используем Dispatcher приоритетом Background
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    if (_isDisposed) return;

                    if (info != null)
                    {
                        _window.Update(info);
                    }
                    else
                    {
                        _window.Clear();
                    }
                }));
        }

        public void Show()
        {
            // НЕ показываем окно сразу! Оно должно быть скрыто до первого трека
            // Просто регистрируем окно, но не показываем
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _nowPlayingService.NowPlayingChanged -= OnNowPlayingChanged;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _window.Close();
            });
        }
    }
}
