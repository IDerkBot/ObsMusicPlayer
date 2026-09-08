using Microsoft.Win32;
using ObsMusicPlayer.Services.Interfaces;

namespace ObsMusicPlayer.Services
{
    public class AutoStartService : IAutoStartService
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MyWpfMusicPlayer";

        public bool IsAutoStartEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
                    return key?.GetValue(AppName) != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void EnableAutoStart()
        {
            try
            {
                var exePath = Environment.ProcessPath;

                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling auto-start: {ex.Message}");
            }
        }

        public void DisableAutoStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                key?.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling auto-start: {ex.Message}");
            }
        }
    }
}
