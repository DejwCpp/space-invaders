using System.Windows;

namespace Space_intruders
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartButton.Click += (s, e) => {
                GameWindow gameWindow = new GameWindow();
                gameWindow.Show();
                this.Close();
            };
        }

        private void LeaderboardButton_Click(object sender, RoutedEventArgs e)
        {
            LeaderboardWindow leaderboard = new LeaderboardWindow();
            leaderboard.Owner = this; // Set owner so it stays on top
            leaderboard.ShowDialog();
        }
    }
}