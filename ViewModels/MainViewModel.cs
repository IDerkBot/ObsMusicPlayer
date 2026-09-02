using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObsMusicPlayer.Models;
using ObsMusicPlayer.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;

namespace ObsMusicPlayer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IPlaylistService _playlistService;
        private readonly IAudioService _audioService;
        private readonly INowPlayingService _nowPlayingService;
        private readonly DispatcherTimer _positionTimer;
        private readonly Random _random = new();
        private NowPlayingInfo? _currentInfo;

        /// <summary>Флаг, чтобы не было рекурсии при перемотке слайдером</summary>
        private bool _isUserSeeking;

        // ─── Коллекции и выбор ────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<Playlist> _playlists = new();

        [ObservableProperty]
        private Playlist? _selectedPlaylist;

        [ObservableProperty]
        private Track? _selectedTrack;

        // ─── Воспроизведение ──────────────────────────────────
        [ObservableProperty] private double _positionSeconds;
        [ObservableProperty] private double _totalSeconds;
        [ObservableProperty] private string _currentTimeText = "0:00";
        [ObservableProperty] private string _totalTimeText = "0:00";
        [ObservableProperty] private float _volume = 0.7f;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private string _nowPlayingTitle = "Ничего не воспроизводится";
        [ObservableProperty] private PlaybackMode _playbackMode = PlaybackMode.Normal;

        public MainViewModel(
            IPlaylistService playlistService,
            IAudioService audioService,
            INowPlayingService nowPlayingService)
        {
            _playlistService = playlistService;
            _audioService = audioService;
            _nowPlayingService = nowPlayingService;

            // Таймер обновления прогресса — 10 раз в секунду
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionTimer.Tick += OnPositionTimerTick;
            _positionTimer.Start();

            _ = LoadDataAsync();
        }

        // ─── Загрузка данных ─────────────────────────────────
        private async Task LoadDataAsync()
        {
            var saved = await _playlistService.LoadPlaylistsAsync();
            Playlists = new ObservableCollection<Playlist>(saved);
        }

        // ─── Таймер позиции ──────────────────────────────────
        private void OnPositionTimerTick(object? sender, EventArgs e)
        {
            TotalSeconds = _audioService.TotalSeconds;
            IsPlaying = _audioService.IsPlaying;

            if (!_isUserSeeking)
            {
                PositionSeconds = _audioService.PositionSeconds;
            }

            CurrentTimeText = FormatTime(PositionSeconds);
            TotalTimeText = FormatTime(TotalSeconds);

            // Обновляем позицию в now playing
            //if (_currentInfo != null && TotalSeconds > 0)
            //{
            //    _currentInfo.Position = TimeSpan.FromSeconds(PositionSeconds);
            //    _currentInfo.Duration = TimeSpan.FromSeconds(TotalSeconds);
            //    _nowPlayingService.UpdateNowPlaying(_currentInfo);
            //}

            if (TotalSeconds > 0 && PositionSeconds >= TotalSeconds - 0.2 && IsPlaying)
            {
                OnTrackFinished();
            }
        }

        // ─── Команды управления плейлистами ──────────────────
        [RelayCommand]
        private async Task AddFolderAsync()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Выберите папку с музыкой" };
            if (dialog.ShowDialog() == true)
            {
                var pl = await _playlistService.CreateFromFolderAsync(dialog.FolderName);
                Playlists.Add(pl);
                await _playlistService.SavePlaylistsAsync(Playlists.ToList());
            }
        }

        [RelayCommand]
        private async Task DeletePlaylistAsync()
        {
            if (SelectedPlaylist == null) return;
            Playlists.Remove(SelectedPlaylist);
            await _playlistService.SavePlaylistsAsync(Playlists.ToList());
        }

        // ─── Команды воспроизведения ─────────────────────────
        [RelayCommand]
        private void PlayPause()
        {
            if (IsPlaying)
            {
                _audioService.Pause();
            }
            else
            {
                // Если есть выбранный трек, но он не загружен в плеер — запускаем
                if (SelectedTrack != null && TotalSeconds == 0)
                    _audioService.Play(SelectedTrack.FilePath);
                else
                    _audioService.Resume();
            }
        }

        [RelayCommand]
        private void StopPlayback()
        {
            _audioService.Stop();
            PositionSeconds = 0;
            TotalSeconds = 0;
            NowPlayingTitle = "Ничего не воспроизводится";
            _nowPlayingService.Clear();
        }

        [RelayCommand]
        private void NextTrack() => PlayNext();

        [RelayCommand]
        private void PreviousTrack()
        {
            // Если прошло больше 3 секунд — перезапуск текущего, иначе — предыдущий
            if (PositionSeconds > 3)
            {
                _audioService.PositionSeconds = 0;
                return;
            }
            PlayPrevious();
        }

        [RelayCommand]
        private void CyclePlaybackMode()
        {
            PlaybackMode = PlaybackMode switch
            {
                PlaybackMode.Normal => PlaybackMode.RepeatAll,
                PlaybackMode.RepeatAll => PlaybackMode.RepeatOne,
                PlaybackMode.RepeatOne => PlaybackMode.Shuffle,
                PlaybackMode.Shuffle => PlaybackMode.Normal,
                _ => PlaybackMode.Normal
            };
        }

        // ─── Реакция на выбор трека в списке ─────────────────
        partial void OnSelectedTrackChanged(Track? value)
        {
            if (value != null)
            {
                _audioService.Play(value.FilePath);
                NowPlayingTitle = value.Title;

                // Ждем немного для загрузки метаданных аудио
                Task.Delay(200).ContinueWith(_ =>
                {
                    _currentInfo = new NowPlayingInfo
                    {
                        Title = value.Title,
                        Artist = ExtractArtist(value),
                        Album = SelectedPlaylist?.Name ?? "",
                        Duration = TimeSpan.FromSeconds(_audioService.TotalSeconds),
                        Position = TimeSpan.Zero,
                        Timestamp = DateTime.Now
                    };

                    _nowPlayingService.UpdateNowPlaying(_currentInfo);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private string ExtractArtist(Track track)
        {
            // Пытаемся извлечь артиста из пути или имени файла
            try
            {
                var directory = Path.GetDirectoryName(track.FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    return Path.GetFileName(directory);
                }
            }
            catch { }

            return "Unknown Artist";
        }

        // ─── Реакция на изменение громкости ──────────────────
        partial void OnVolumeChanged(float value)
        {
            _audioService.Volume = value;
        }

        // ─── Перемотка (вызывается из View при отпуске слайдера) ──
        [RelayCommand]
        private void SeekToPosition()
        {
            _audioService.PositionSeconds = PositionSeconds;
        }

        public void BeginSeek() => _isUserSeeking = true;
        public void EndSeek()
        {
            _isUserSeeking = false;
            _audioService.PositionSeconds = PositionSeconds;
        }

        // ─── Логика навигации ────────────────────────────────
        private void OnTrackFinished()
        {
            switch (PlaybackMode)
            {
                case PlaybackMode.RepeatOne:
                    _audioService.PositionSeconds = 0;
                    _audioService.Resume();
                    break;
                default:
                    PlayNext();
                    break;
            }
        }

        private void PlayNext()
        {
            var tracks = SelectedPlaylist?.Tracks;
            if (tracks == null || tracks.Count == 0) return;

            int currentIndex = SelectedTrack != null ? tracks.IndexOf(SelectedTrack) : -1;

            int nextIndex = PlaybackMode switch
            {
                PlaybackMode.Shuffle => _random.Next(tracks.Count),
                _ => currentIndex + 1
            };

            if (nextIndex >= tracks.Count)
            {
                if (PlaybackMode == PlaybackMode.RepeatAll)
                    nextIndex = 0;
                else
                {
                    _audioService.Stop();
                    return;
                }
            }

            SelectedTrack = tracks[nextIndex];
        }

        private void PlayPrevious()
        {
            var tracks = SelectedPlaylist?.Tracks;
            if (tracks == null || tracks.Count == 0) return;

            int currentIndex = SelectedTrack != null ? tracks.IndexOf(SelectedTrack) : 0;
            int prevIndex = currentIndex - 1;

            if (prevIndex < 0)
                prevIndex = PlaybackMode == PlaybackMode.RepeatAll ? tracks.Count - 1 : 0;

            SelectedTrack = tracks[prevIndex];
        }

        // ─── Утилиты ─────────────────────────────────────────
        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0) return "0:00";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }
}
