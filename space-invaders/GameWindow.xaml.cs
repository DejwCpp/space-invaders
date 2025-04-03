using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Media;

namespace Space_intruders
{
    public partial class GameWindow : Window
    {
        // --- Game State Variables ---
        int marginPoz = 370;
        public Player player = new();
        static int counter = 0; // Arrow counter
        public Dictionary<int, Arrow> arrows = new Dictionary<int, Arrow>();
        public List<Shield> shields = new List<Shield>();
        public Enemies enemies { get; private set; }
        bool canShoot = true;
        private int currentScore = 0;
        private Random random = new Random();

        // --- Boost Related Variables ---
        private List<Boost> activeBoostsOnScreen = new List<Boost>();
        private const double BoostSpawnChance = 0.10; // 10% chance for a boost to drop on enemy death
        // *** FOR TESTING: Temporarily set to 1.0 (100%) ***
        // private const double BoostSpawnChance = 1.0;
        private const int MaxPlayerHp = 5;

        // Faster Shooting Boost State
        private int baseShootCooldownMs = 500;
        private int currentShootCooldownMs;
        private DispatcherTimer fasterShootingTimer;
        private const int FasterShootingDurationSeconds = 10;
        private const int FasterShootCooldownMs = 150;

        // Temporary Armor Boost State
        public bool isArmorActive { get; private set; } = false; // Public for EnemyProjectile collision check
        private DispatcherTimer armorTimer;
        private const int ArmorDurationSeconds = 8;
        private Image armorIndicator; // Optional visual feedback

        // Slow Enemies Boost State
        public bool areEnemiesSlowed { get; private set; } = false; // Public for Enemies class speed adjustment
        private DispatcherTimer slowEnemiesTimer;
        private const int SlowEnemiesDurationSeconds = 12;

        // --- Constructor & Initialization ---
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
            InitializeBoostTimers();
            currentShootCooldownMs = baseShootCooldownMs;
        }

        private void InitializePlayer()
        {
            player.SetID(0);
            player.SetHP(3);
            player.SetSpeed(20);
            player.SetArmour(1);
            player.SetDMG(1);
        }

        private void InitializeBoostTimers()
        {
            fasterShootingTimer = new DispatcherTimer();
            fasterShootingTimer.Interval = TimeSpan.FromSeconds(FasterShootingDurationSeconds);
            fasterShootingTimer.Tick += FasterShootingTimer_Tick;

            armorTimer = new DispatcherTimer();
            armorTimer.Interval = TimeSpan.FromSeconds(ArmorDurationSeconds);
            armorTimer.Tick += ArmorTimer_Tick;

            slowEnemiesTimer = new DispatcherTimer();
            slowEnemiesTimer.Interval = TimeSpan.FromSeconds(SlowEnemiesDurationSeconds);
            slowEnemiesTimer.Tick += SlowEnemiesTimer_Tick;
        }

        // --- Boost Spawning ---
        public void TrySpawnBoost(double x, double y)
        {
            // Check if the random chance is met
            if (random.NextDouble() < BoostSpawnChance)
            {
                // *** ADDED DEBUG LINE ***
                Debug.WriteLine($"TrySpawnBoost: Chance met. Attempting to determine boost type at ({x:F0}, {y:F0}).");

                Array boostTypes = Enum.GetValues(typeof(Boost.BoostType));
                Boost.BoostType randomType = (Boost.BoostType)boostTypes.GetValue(random.Next(boostTypes.Length));

                // *** ADDED DEBUG LINE ***
                Debug.WriteLine($"TrySpawnBoost: Determined type: {randomType}. Creating Boost object...");

                Boost newBoost = new Boost(gameCanvas, x, y, randomType, this);
                if (newBoost.BoostImage != null) // Check if boost constructor succeeded (image loaded etc.)
                {
                    activeBoostsOnScreen.Add(newBoost);
                    // This confirms the boost object was created and added
                    Debug.WriteLine($"TrySpawnBoost: Successfully created and added boost: {randomType}.");
                }
                else
                {
                    // This indicates the Boost constructor failed (likely image loading)
                    Debug.WriteLine($"TrySpawnBoost: Failed to create Boost object for type {randomType}. BoostImage was null.");
                }
            }
            // (Optional) Add an else block here if you want to see when the chance is *not* met
            // else
            // {
            //     Debug.WriteLine($"TrySpawnBoost: Chance {BoostSpawnChance:P0} not met.");
            // }
        }

        // --- Boost Activation ---
        public void ActivateBoost(Boost.BoostType type)
        {
            Debug.WriteLine($"Activating boost: {type}");
            switch (type)
            {
                case Boost.BoostType.FasterShooting: ApplyFasterShooting(); break;
                case Boost.BoostType.HealthRegen: ApplyHealthRegen(); break;
                case Boost.BoostType.TemporaryArmor: ApplyTemporaryArmor(); break;
                case Boost.BoostType.SlowEnemies: ApplySlowEnemies(); break;
            }
        }

        // --- Individual Boost Logic & Timers ---

        private void ApplyFasterShooting()
        {
            currentShootCooldownMs = FasterShootCooldownMs;
            fasterShootingTimer.Stop();
            fasterShootingTimer.Start();
            Debug.WriteLine($"Faster shooting activated! Cooldown: {currentShootCooldownMs}ms");
        }

        private void FasterShootingTimer_Tick(object sender, EventArgs e)
        {
            fasterShootingTimer.Stop();
            currentShootCooldownMs = baseShootCooldownMs;
            Debug.WriteLine("Faster shooting expired. Cooldown reset.");
        }

        private void ApplyHealthRegen()
        {
            if (player.GetHP() < MaxPlayerHp)
            {
                player.SetHP(player.GetHP() + 1);
                UpdateHeartDisplay();
                Debug.WriteLine($"Health boost collected! HP: {player.GetHP()}");
            }
            else
            {
                Debug.WriteLine("Health boost collected, but HP already full.");
                AddScore(25); // Give points instead
            }
        }

        private void ApplyTemporaryArmor()
        {
            isArmorActive = true;
            armorTimer.Stop();
            armorTimer.Start();
            Debug.WriteLine("Armor activated!");
            ShowArmorIndicator(true);
        }

        private void ArmorTimer_Tick(object sender, EventArgs e)
        {
            armorTimer.Stop();
            isArmorActive = false;
            Debug.WriteLine("Armor expired.");
            ShowArmorIndicator(false);
        }

        private void ShowArmorIndicator(bool show)
        {
            Dispatcher.Invoke(() => {
                if (show)
                {
                    if (armorIndicator == null)
                    {
                        try
                        {
                            armorIndicator = new Image
                            {
                                Width = playerImage.Width + 10,
                                Height = playerImage.Height + 10,
                                Source = new BitmapImage(new Uri("/Resources/shield_indicator.png", UriKind.Relative)),
                                Opacity = 0.7,
                                Stretch = Stretch.Fill
                            };
                            Canvas.SetZIndex(armorIndicator, Canvas.GetZIndex(playerImage) - 1);
                            gameCanvas.Children.Add(armorIndicator);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading shield indicator: {ex.Message}");
                            armorIndicator = null;
                        }
                    }
                    if (armorIndicator != null)
                    {
                        Canvas.SetLeft(armorIndicator, Canvas.GetLeft(playerImage) - 5);
                        Canvas.SetTop(armorIndicator, Canvas.GetTop(playerImage) - 5);
                        armorIndicator.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (armorIndicator != null)
                    {
                        armorIndicator.Visibility = Visibility.Hidden;
                    }
                }
            });
        }

        private void ApplySlowEnemies()
        {
            if (!areEnemiesSlowed)
            {
                areEnemiesSlowed = true;
                enemies?.SlowDownEnemies(); // Notify Enemies class
            }
            slowEnemiesTimer.Stop();
            slowEnemiesTimer.Start();
            Debug.WriteLine("Enemies slowed!");
        }

        private void SlowEnemiesTimer_Tick(object sender, EventArgs e)
        {
            slowEnemiesTimer.Stop();
            if (areEnemiesSlowed)
            {
                areEnemiesSlowed = false;
                enemies?.SpeedUpEnemies(); // Notify Enemies class
                Debug.WriteLine("Enemy slow expired.");
            }
        }

        // --- UI Update & Player Control ---

        private void InitializeHeartImages()
        {
            heartsPanel.Children.Clear();
            int heartsToShow = Math.Max(0, player.GetHP());
            for (int i = 0; i < heartsToShow; i++)
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

        // Handles player movement and shooting input
        public async void PlayerMovement(object sender, KeyEventArgs e)
        {
            if (playerImage == null) return;

            double playerWidth = playerImage.Width;
            double canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            double playerLeft = Canvas.GetLeft(playerImage);
            double playerTop = Canvas.GetTop(playerImage);

            switch (e.Key)
            {
                case Key.Right:
                case Key.D:
                    if (playerLeft + player.GetSpeed() + playerWidth <= canvasWidth)
                    {
                        playerLeft += player.GetSpeed();
                        Canvas.SetLeft(playerImage, playerLeft);
                    }
                    break;
                case Key.Left:
                case Key.A:
                    if (playerLeft - player.GetSpeed() >= 0)
                    {
                        playerLeft -= player.GetSpeed();
                        Canvas.SetLeft(playerImage, playerLeft);
                    }
                    break;
                case Key.Space:
                    if (canShoot)
                    {
                        canShoot = false;
                        SpawnNewArrow();
                        await Task.Delay(currentShootCooldownMs); // Use dynamic cooldown
                        canShoot = true;
                    }
                    break;
            }

            // Update armor indicator position
            if (armorIndicator != null && armorIndicator.Visibility == Visibility.Visible)
            {
                Canvas.SetLeft(armorIndicator, playerLeft - 5);
                Canvas.SetTop(armorIndicator, playerTop - 5);
            }
        }

        public void UpdateHeartDisplay()
        {
            Dispatcher.Invoke(() => { InitializeHeartImages(); });
        }

        // --- Game Over Handling ---
        public void GameOver(bool won = false)
        {
            this.PreviewKeyDown -= PlayerMovement; // Stop player input

            // Stop all active timers
            fasterShootingTimer?.Stop();
            armorTimer?.Stop();
            slowEnemiesTimer?.Stop();
            enemies?.StopTimers();
            foreach (var arrow in arrows.Values.ToList()) arrow.StopTimer();
            arrows.Clear();

            // Cleanup visuals on UI thread
            Dispatcher.Invoke(() => {
                foreach (var boost in activeBoostsOnScreen.ToList()) boost.Cleanup();
                activeBoostsOnScreen.Clear();

                // Remove lingering visuals
                foreach (var arrowImg in gameCanvas.Children.OfType<Image>().Where(img => img.Tag is Arrow).ToList())
                    gameCanvas.Children.Remove(arrowImg);
                foreach (var projImg in gameCanvas.Children.OfType<Image>().Where(img => img.Tag is EnemyProjectile).ToList()) // Requires Tagging projectiles
                    gameCanvas.Children.Remove(projImg);
                foreach (var enemyImg in enemies?.enemies.ToList() ?? new List<Image>())
                    if (gameCanvas.Children.Contains(enemyImg)) gameCanvas.Children.Remove(enemyImg);

                // Display Game Over/Win message
                Label gameOverLabel = new Label
                {
                    Content = won ? "YOU WIN!" : "GAME OVER",
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    Foreground = won ? Brushes.Gold : Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                double labelWidthEstimate = 350; double labelHeightEstimate = 80;
                Canvas.SetLeft(gameOverLabel, (gameCanvas.ActualWidth - labelWidthEstimate) / 2);
                Canvas.SetTop(gameOverLabel, (gameCanvas.ActualHeight - labelHeightEstimate) / 2);
                Canvas.SetZIndex(gameOverLabel, 100);

                if (!gameCanvas.Children.OfType<Label>().Any(lbl => lbl.Content.ToString().Contains("GAME OVER") || lbl.Content.ToString().Contains("YOU WIN")))
                    gameCanvas.Children.Add(gameOverLabel);
            });

            Debug.WriteLine($"Game Over called. Won: {won}");
        }

        // --- Arrow Handling ---
        public void SpawnNewArrow()
        {
            try
            {
                if (playerImage == null)
                {
                    Debug.WriteLine("Error: Cannot spawn arrow, player image is null."); return;
                }

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
                    Tag = arrow // Tag Image for easier cleanup in GameOver
                };

                double playerTop = Canvas.GetTop(playerImage);
                double playerLeft = Canvas.GetLeft(playerImage);
                Canvas.SetLeft(arrowImage, playerLeft + playerImage.Width / 2 - arrowImage.Width / 2);
                Canvas.SetTop(arrowImage, playerTop - arrowImage.Height);

                gameCanvas.Children.Add(arrowImage);
                arrow.SetImageSource(arrowImage);
                arrows.Add(arrow.GetID(), arrow);
                arrow.SetTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating arrow: {ex.Message} \nStackTrace: {ex.StackTrace}");
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

        // --- Shield & Score Handling ---
        private void InitializeShields()
        {
            Dispatcher.Invoke(() => {
                foreach (var shield in shields)
                {
                    Image shieldImg = shield.GetImage();
                    if (shieldImg != null && gameCanvas.Children.Contains(shieldImg))
                        gameCanvas.Children.Remove(shieldImg);
                }
                shields.Clear();

                double[] shieldPositions = { 100, 250, 400, 550, 700 };
                double shieldPosY = 470;

                foreach (double posX in shieldPositions)
                {
                    shields.Add(new Shield(gameCanvas, posX, shieldPosY));
                }
            });
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
                Canvas.SetLeft(ScoreLabel, (gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800) - 150);
                Canvas.SetTop(ScoreLabel, 10);
                ScoreLabel.FontSize = 16;
                ScoreLabel.Foreground = Brushes.White;
                Canvas.SetZIndex(ScoreLabel, 50);
            });
        }
    }
}