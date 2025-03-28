using System.Windows;

namespace Space_intruders;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    GameWindow gameWindow = new GameWindow();
    private Enemies enemies;
    public MainWindow()
    {
        InitializeComponent();
        StartButton.Click += (s, e) => {
            gameWindow.Show();
            enemies = new Enemies(gameWindow.gameCanvas);
            this.Close();
        };
    }
}