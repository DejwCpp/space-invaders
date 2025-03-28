using System.Windows;

namespace Space_intruders;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StartButton.Click += (s, e) => {
            GameWindow gameWindow = new GameWindow();
            Application.Current.MainWindow = gameWindow;
            gameWindow.Show();
            Enemies enemies;
            enemies = new Enemies(gameWindow.gameCanvas, gameWindow);
            this.Close();
        };
    }
}