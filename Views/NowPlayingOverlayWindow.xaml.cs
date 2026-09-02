using ObsMusicPlayer.Models;
using System.Windows;
using System.Windows.Threading;

namespace ObsMusicPlayer.Views
{
    /// <summary>
    /// Логика взаимодействия для NowPlayingOverlayWindow.xaml
    /// </summary>
    public partial class NowPlayingOverlayWindow : Window
    {
        private DispatcherTimer? _hideTimer;
        private bool _isVisible;

        public NowPlayingOverlayWindow()
        {
            InitializeComponent();

            // Скрываем окно при старте
            Hide();

            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5),
                IsEnabled = false
            };

            _hideTimer.Tick += (s, e) =>
            {
                HideOverlay();
            };
        }

        public void Update(NowPlayingInfo info)
        {
            // Обновляем данные
            TitleText.Text = info.Title;
            ArtistText.Text = info.Artist;
            ProgressText.Text = info.ProgressText;

            // Показываем окно
            ShowOverlay();

            // Перезапускаем таймер скрытия
            _hideTimer?.Stop();
            _hideTimer?.Start();
        }

        public void Clear()
        {
            HideOverlay();
            _hideTimer?.Stop();
        }

        private void ShowOverlay()
        {
            if (!_isVisible)
            {
                Show();
                _isVisible = true;

                // Анимация появления (опционально)
                Opacity = 0;
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void HideOverlay()
        {
            if (_isVisible)
            {
                // Анимация исчезновения
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                fadeOut.Completed += (s, e) =>
                {
                    Hide();
                    _isVisible = false;
                    Opacity = 1; // Сбрасываем для следующего показа
                };
                BeginAnimation(OpacityProperty, fadeOut);
            }
        }
    }
}
