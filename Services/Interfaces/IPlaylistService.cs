using ObsMusicPlayer.Models;

namespace ObsMusicPlayer.Services.Interfaces
{
    public interface IPlaylistService
    {
        Task<List<Playlist>> LoadPlaylistsAsync();
        Task SavePlaylistsAsync(List<Playlist> playlists);
        Task<Playlist> CreateFromFolderAsync(string folderPath);
    }
}
