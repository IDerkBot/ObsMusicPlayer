namespace ObsMusicPlayer.Models
{
    public class Track
    {
        public string FilePath { get; set; } = string.Empty;

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _fullName = "Unknown";
                }

                _fullName = value;

                var data = _fullName.Split(" - ");
                if (data.Length < 2)
                {
                    return;
                }

                Artist = data[0];
                Title = data[1];
            }
        }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
    }
}
