using ObsMusicPlayer.Models;
using ObsMusicPlayer.Services.Interfaces;
using System.IO;
using System.Text.Json;

namespace ObsMusicPlayer.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly string _saveFilePath;
        private readonly string[] _audioExtensions = { ".mp3", ".wav", ".flac", ".m4a" };

        public PlaylistService()
        {
            // Сохраняем в AppData пользователя
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "MyWpfMusicPlayer");
            Directory.CreateDirectory(folder);
            _saveFilePath = Path.Combine(folder, "playlists.json");
        }

        public async Task<List<Playlist>> LoadPlaylistsAsync()
        {
            if (!File.Exists(_saveFilePath)) return new List<Playlist>();

            var json = await File.ReadAllTextAsync(_saveFilePath);
            var playlists = JsonSerializer.Deserialize<List<Playlist>>(json) ?? new();

            // При загрузке сразу сканируем папки, чтобы заполнить треки
            foreach (var pl in playlists)
            {
                await ScanFolderForTracksAsync(pl);
            }
            return playlists;
        }

        public async Task SavePlaylistsAsync(List<Playlist> playlists)
        {
            var json = JsonSerializer.Serialize(playlists, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_saveFilePath, json);
        }

        public async Task<Playlist> CreateFromFolderAsync(string folderPath)
        {
            var playlist = new Playlist
            {
                Name = new DirectoryInfo(folderPath).Name,
                FolderPath = folderPath
            };
            await ScanFolderForTracksAsync(playlist);
            return playlist;
        }

        private async Task ScanFolderForTracksAsync(Playlist playlist)
        {
            playlist.Tracks.Clear();
            if (!Directory.Exists(playlist.FolderPath)) return;

            await Task.Run(() =>
            {
                var files = Directory.GetFiles(playlist.FolderPath)
                    .Where(f => _audioExtensions.Contains(Path.GetExtension(f).ToLower()));

                foreach (var file in files)
                {
                    playlist.Tracks.Add(new Track
                    {
                        FilePath = file,
                        Title = Path.GetFileNameWithoutExtension(file)
                    });
                }
            });
        }
    }
}
