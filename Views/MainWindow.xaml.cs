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
    }
}