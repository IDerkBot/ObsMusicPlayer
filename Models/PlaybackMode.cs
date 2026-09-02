namespace ObsMusicPlayer.Models
{
    public enum PlaybackMode
    {
        Normal,      // Обычный порядок, остановка в конце
        RepeatAll,   // Циклически весь плейлист
        RepeatOne,   // Повтор текущего трека
        Shuffle      // Случайный порядок
    }
}
