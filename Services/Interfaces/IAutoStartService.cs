namespace ObsMusicPlayer.Services.Interfaces
{
    public interface IAutoStartService
    {
        bool IsAutoStartEnabled { get; }
        void EnableAutoStart();
        void DisableAutoStart();
    }
}
