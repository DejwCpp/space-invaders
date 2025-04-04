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
                this.Close(); // Close the MainWindow
            };
        }
    }
}