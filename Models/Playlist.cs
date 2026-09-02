namespace ObsMusicPlayer.Models
{
    public class Playlist
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;

        // Не сериализуем, так как треки мы будем подгружать из папки
        [System.Text.Json.Serialization.JsonIgnore]
        public List<Track> Tracks { get; set; } = new();
    }
}
