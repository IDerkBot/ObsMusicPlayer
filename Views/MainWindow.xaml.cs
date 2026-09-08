using ObsMusicPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ObsMusicPlayer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private MainViewModel? VM => DataContext as MainViewModel;

        private void SeekSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            VM?.BeginSeek();
        }

        private void SeekSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            VM?.EndSeek();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Останавливаем воспроизведение
            if (DataContext is MainViewModel vm)
            {
                vm.StopPlaybackCommand.Execute(null);
            }

            // Явно завершаем приложение
            Application.Current.Shutdown();
        }
    }
}