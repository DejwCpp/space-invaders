using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace Space_intruders
{
    public partial class GameWindow : Window
    {
        int marginPoz = 370;
        public Player player = new();
        static int counter = 0; 
        public Dictionary<int, Arrow> arrows = new Dictionary<int, Arrow>();
        public List<Shield> shields = new List<Shield>();
        public Enemies enemies { get; private set; }
        bool canShoot = true;
        private int currentScore = 0; 

        public GameWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(PlayerMovement);
            InitializePlayer();
            InitializeShields();
            InitializeHeartImages();
            enemies = new Enemies(gameCanvas, this); 
            enemies.InitializeEnemies();
            UpdateScoreDisplay();
        }

        private void InitializePlayer()
        {
            player.SetID(0);
            player.SetHP(3);
            player.SetSpeed(20);
            player.SetArmour(1);
            player.SetDMG(1);
        }
        private void InitializeShields()
        {
            // Clear existing shield images if any (for potential restarts)
            foreach (var shield in shields)
            {
                gameCanvas.Children.Remove(shield.GetImage());
            }
            shields.Clear();

            double[] shieldPositions = { 100, 250, 400, 550, 700 };
            double shieldPosY = 470; 

            foreach (double posX in shieldPositions)
            {
                shields.Add(new Shield(gameCanvas, posX, shieldPosY));
            }
        }
        private void InitializeHeartImages()
        {
            heartsPanel.Children.Clear(); // Clear existing hearts first
            for (int i = 0; i < player.GetHP(); i++)
            {
                Image heartImage = new Image()
                {
                    Source = new BitmapImage(new Uri("/Resources/heart.png", UriKind.Relative)),
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                Canvas.SetZIndex(heartImage, 2); 
                heartsPanel.Children.Add(heartImage);
            }
            Canvas.SetZIndex(heartsPanel, 50); 
        }

        public void SpawnNewArrow()
        {
            try
            {
                Arrow arrow = new Arrow();
                Interlocked.Increment(ref counter);
                arrow.SetID(counter);
                arrow.SetDMG(player.GetDMG());
                arrow.SetSpeed(10);
                arrow.SetGameWindow(this);

                BitmapImage imageSource = new BitmapImage(new Uri("/Resources/arrow.png", UriKind.Relative));
                Image arrowImage = new Image()
                {
                    Source = imageSource,
                    Width = 30, 
                    Height = 30,
                };

                double playerTop = Canvas.GetTop(playerImage);
                // Ensure playerImage exists before accessing properties
                if (playerImage != null)
                {
                    Canvas.SetLeft(arrowImage, Canvas.GetLeft(playerImage) + playerImage.Width / 2 - arrowImage.Width / 2);
                    Canvas.SetTop(arrowImage, playerTop - arrowImage.Height);

                    gameCanvas.Children.Add(arrowImage);
                    arrow.SetImageSource(arrowImage);
                    arrows.Add(arrow.GetID(), arrow);
                    arrow.SetTimer();
                }
                else
                {
                    Debug.WriteLine("Error: Player image not found when spawning arrow.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating arrow: " + ex.Message);
            }
        }

        public void RemoveArrow(int arrowID)
        {
            Dispatcher.Invoke(() => {
                if (arrows.TryGetValue(arrowID, out Arrow arrow))
                {
                    Image arrowImage = arrow.GetImageSource();
                    if (arrowImage != null && gameCanvas.Children.Contains(arrowImage))
                    {
                        gameCanvas.Children.Remove(arrowImage);
                    }
                    arrows.Remove(arrowID);
                }
            });
        }


        // Player Movement
        public async void PlayerMovement(object sender, KeyEventArgs e)
        {
            double playerWidth = playerImage.Width;
            double canvasWidth = gameCanvas.ActualWidth; 

            switch (e.Key)
            {
                case Key.Right:
                case Key.D:
                    // Prevent moving off the right edge
                    if (marginPoz + player.GetSpeed() + playerWidth <= canvasWidth)
                    {
                        marginPoz += player.GetSpeed();
                        Canvas.SetLeft(playerImage, marginPoz);
                    }
                    break;
                case Key.Left:
                case Key.A:
                    // Prevent moving off the left edge
                    if (marginPoz - player.GetSpeed() >= 0)
                    {
                        marginPoz -= player.GetSpeed();
                        Canvas.SetLeft(playerImage, marginPoz);
                    }
                    break;
                case Key.Space:
                    if (canShoot)
                    {
                        canShoot = false;
                        SpawnNewArrow();
                        await Task.Delay(500);
                        canShoot = true;
                    }
                    break;
            }
        }
        public void AddScore(int points)
        {
            currentScore += points;
            UpdateScoreDisplay();
        }

        private void UpdateScoreDisplay()
        {
            Dispatcher.Invoke(() => {
                ScoreLabel.Content = $"Score: {currentScore}";

                Canvas.SetLeft(ScoreLabel, gameCanvas.Width - 150); 
                Canvas.SetTop(ScoreLabel, 10);
                ScoreLabel.FontSize = 16;
                ScoreLabel.Foreground = System.Windows.Media.Brushes.White; 
                Canvas.SetZIndex(ScoreLabel, 50);
            });
        }

        public void UpdateHeartDisplay()
        {
            Dispatcher.Invoke(() => {
                InitializeHeartImages(); 
            });
        }

        // Game Over Handling (basic, needed upgrade)
        public void GameOver(bool won = false)
        {
            Dispatcher.Invoke(() => {
                // Stop all game activity
                enemies?.StopTimers();
                foreach (var arrow in arrows.Values.ToList()) 
                    arrow.StopTimer(); 
            });
        }
    }
}