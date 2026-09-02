using Microsoft.Extensions.DependencyInjection;
using ObsMusicPlayer.Services;
using ObsMusicPlayer.Services.Interfaces;
using ObsMusicPlayer.ViewModels;
using ObsMusicPlayer.Views;
using System.Windows;

namespace ObsMusicPlayer
{
    public partial class App : Application
    {
        // Глобальный провайдер сервисов
        public static IServiceProvider Services { get; private set; } = null!;
        private OverlayWindowManager? _overlayManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // Создаем и показываем overlay менеджер
            _overlayManager = Services.GetRequiredService<OverlayWindowManager>();

            // Запускаем главное окно, разрешая его зависимости
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Регистрируем сервисы (Singleton, т.к. они хранят состояние или ресурсы)
            services.AddSingleton<IPlaylistService, PlaylistService>();
            services.AddSingleton<IAudioService, AudioService>();
            services.AddSingleton<INowPlayingService, NowPlayingService>();

            services.AddSingleton<OverlayWindowManager>();

            // Регистрируем ViewModel (Transient или Singleton, зависит от нужд. Для главного окна - Singleton)
            services.AddSingleton<MainViewModel>();

            // Регистрируем View и инжектим ViewModel
            services.AddTransient<MainWindow>(provider =>
            {
                var vm = provider.GetRequiredService<MainViewModel>();
                return new MainWindow { DataContext = vm };
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _overlayManager?.Dispose();

            if (Services.GetService<IAudioService>() is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (Services.GetService<INowPlayingService>() is IDisposable nowPlayingDisposable)
            {
                nowPlayingDisposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}
