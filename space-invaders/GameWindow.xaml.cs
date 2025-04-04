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
        public Player player = new();
        static int counter = 0;
        public Dictionary<int, Arrow> arrows = new Dictionary<int, Arrow>();
        public List<Shield> shields = new List<Shield>();
        public Enemies enemies { get; private set; }
        bool canShoot = true;
        private int currentScore = 0;
        private Random random = new Random();

        // --- Input State for Smooth Movement & Shooting ---
        private bool isMovingLeft = false;
        private bool isMovingRight = false;
        private bool isShootingKeyDown = false; // Track if spacebar is held

        // --- Game Loop Timer ---
        private DispatcherTimer gameLoopTimer;

        // --- Boost Related Variables ---
        private List<Boost> activeBoostsOnScreen = new List<Boost>();
        private const double BoostSpawnChance = 0.10;
        private const int MaxPlayerHp = 5;

        // Faster Shooting Boost State
        private int baseShootCooldownMs = 500;
        private int currentShootCooldownMs;
        private DispatcherTimer fasterShootingTimer;
        private const int FasterShootingDurationSeconds = 10;
        private const int FasterShootCooldownMs = 150;

        // Temporary Armor Boost State
        public bool isArmorActive { get; private set; } = false;
        private DispatcherTimer armorTimer;
        private const int ArmorDurationSeconds = 8;
        private Image armorIndicator;

        // Slow Enemies Boost State
        public bool areEnemiesSlowed { get; private set; } = false;
        private DispatcherTimer slowEnemiesTimer;
        private const int SlowEnemiesDurationSeconds = 12;

        private const double GameAreaWidth = 800;
        private const double GameAreaHeight = 580;

        // --- Constructor & Initialization ---
        public GameWindow()
        {
            InitializeComponent();

            this.KeyDown += GameWindow_KeyDown;
            this.KeyUp += GameWindow_KeyUp;

            InitializePlayer();
            InitializeShields();
            InitializeHeartImages();
            enemies = new Enemies(gameCanvas, this);
            enemies.InitializeEnemies();
            UpdateScoreDisplay();
            InitializeBoostTimers();
            InitializeGameLoop();
            currentShootCooldownMs = baseShootCooldownMs;
        }

        private void InitializePlayer()
        {
            player.SetID(0);
            player.SetHP(3);
            player.SetSpeed(5); // Adjust this for desired speed with timer
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

        private void InitializeGameLoop()
        {
            gameLoopTimer = new DispatcherTimer();
            gameLoopTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            gameLoopTimer.Tick += GameLoopTick;
            gameLoopTimer.Start();
        }

        // --- Main Game Loop ---
        private void GameLoopTick(object sender, EventArgs e)
        {
            MovePlayer(); // Process movement based on flags
            TryShoot();   // Process shooting based on flags & cooldown
        }

        // --- Input Event Handlers ---
        private void GameWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Use non-repeating key check if needed, but usually fine for games
            // if (e.IsRepeat) return;

            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    isMovingLeft = true;
                    break;
                case Key.Right:
                case Key.D:
                    isMovingRight = true;
                    break;
                case Key.Space:
                    isShootingKeyDown = true; // Set flag when space is pressed
                    break;
            }
        }

        private void GameWindow_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    isMovingLeft = false;
                    break;
                case Key.Right:
                case Key.D:
                    isMovingRight = false;
                    break;
                case Key.Space:
                    isShootingKeyDown = false; // Clear flag when space is released
                    break;
            }
        }

        // --- Player Movement Logic (Called by Game Loop) ---
        private void MovePlayer()
        {
            if (playerImage == null) return;

            double currentLeft = Canvas.GetLeft(playerImage);
            double newLeft = currentLeft;
            int speed = player.GetSpeed();

            if (isMovingLeft && !isMovingRight)
            {
                newLeft = currentLeft - speed;
                if (newLeft < 0) newLeft = 0;
            }
            else if (isMovingRight && !isMovingLeft)
            {
                newLeft = currentLeft + speed;
                if (newLeft + playerImage.Width > GameAreaWidth)
                    newLeft = GameAreaWidth - playerImage.Width;
            }

            if (newLeft != currentLeft)
            {
                Canvas.SetLeft(playerImage, newLeft);
                UpdateArmorIndicatorPosition();
            }
        }

        // --- Shooting Logic (Called by Game Loop) ---
        private void TryShoot()
        {
            // Check if the shoot key is held AND the cooldown has finished
            if (isShootingKeyDown && canShoot)
            {
                canShoot = false;         // Prevent firing again until cooldown finishes
                SpawnNewArrow();          // Fire the arrow
                StartShootCooldown();     // Start the async cooldown timer
            }
        }

        // --- Starts the cooldown asynchronously ---
        private async void StartShootCooldown()
        {
            await Task.Delay(currentShootCooldownMs); // Wait for the cooldown duration
            canShoot = true; // Allow shooting again
        }

        // --- Boost Spawning ---
        public void TrySpawnBoost(double x, double y)
        {
            if (random.NextDouble() < BoostSpawnChance)
            {
                Debug.WriteLine($"TrySpawnBoost: Chance met. Attempting to determine boost type at ({x:F0}, {y:F0}).");
                Array boostTypes = Enum.GetValues(typeof(Boost.BoostType));
                Boost.BoostType randomType = (Boost.BoostType)boostTypes.GetValue(random.Next(boostTypes.Length));
                Debug.WriteLine($"TrySpawnBoost: Determined type: {randomType}. Creating Boost object...");

                Boost newBoost = new Boost(gameCanvas, x, y, randomType, this);
                if (newBoost.BoostImage != null)
                {
                    activeBoostsOnScreen.Add(newBoost);
                    Debug.WriteLine($"TrySpawnBoost: Successfully created and added boost: {randomType}.");
                }
                else
                {
                    Debug.WriteLine($"TrySpawnBoost: Failed to create Boost object for type {randomType}. BoostImage was null.");
                }
            }
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
                AddScore(25);
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
                    if (armorIndicator == null && playerImage != null)
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
                            UpdateArmorIndicatorPosition();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading shield indicator: {ex.Message}");
                            armorIndicator = null;
                        }
                    }
                    if (armorIndicator != null)
                    {
                        armorIndicator.Visibility = Visibility.Visible;
                        UpdateArmorIndicatorPosition();
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

        private void UpdateArmorIndicatorPosition()
        {
            if (armorIndicator != null && armorIndicator.Visibility == Visibility.Visible && playerImage != null)
            {
                Canvas.SetLeft(armorIndicator, Canvas.GetLeft(playerImage) - 5);
                Canvas.SetTop(armorIndicator, Canvas.GetTop(playerImage) - 5);
            }
        }

        private void ApplySlowEnemies()
        {
            if (!areEnemiesSlowed)
            {
                areEnemiesSlowed = true;
                enemies?.SlowDownEnemies();
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
                enemies?.SpeedUpEnemies();
                Debug.WriteLine("Enemy slow expired.");
            }
        }

        // --- UI Update ---
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

        public void UpdateHeartDisplay()
        {
            Dispatcher.Invoke(() => { InitializeHeartImages(); });
        }

        // --- Game Over Handling ---
        public void GameOver(bool won = false)
        {
            this.KeyDown -= GameWindow_KeyDown;
            this.KeyUp -= GameWindow_KeyUp;

            gameLoopTimer?.Stop();
            fasterShootingTimer?.Stop();
            armorTimer?.Stop();
            slowEnemiesTimer?.Stop();
            enemies?.StopTimers();
            foreach (var arrow in arrows.Values.ToList()) arrow.StopTimer();
            arrows.Clear();

            Dispatcher.Invoke(() => {
                foreach (var boost in activeBoostsOnScreen.ToList()) boost.Cleanup();
                activeBoostsOnScreen.Clear();

                foreach (var arrowImg in gameCanvas.Children.OfType<Image>().Where(img => img.Tag is Arrow).ToList())
                    gameCanvas.Children.Remove(arrowImg);
                foreach (var projImg in gameCanvas.Children.OfType<Image>().Where(img => img.Tag is EnemyProjectile).ToList())
                    gameCanvas.Children.Remove(projImg);
                foreach (var enemyImg in enemies?.enemies.ToList() ?? new List<Image>())
                    if (gameCanvas.Children.Contains(enemyImg)) gameCanvas.Children.Remove(enemyImg);

                if (playerImage != null) playerImage.Visibility = Visibility.Collapsed;
                if (armorIndicator != null) armorIndicator.Visibility = Visibility.Collapsed;

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
                Canvas.SetLeft(gameOverLabel, (GameAreaWidth - labelWidthEstimate) / 2);
                Canvas.SetTop(gameOverLabel, (GameAreaHeight - labelHeightEstimate) / 2);
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
                if (playerImage == null || playerImage.Visibility != Visibility.Visible)
                {
                    return;
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
                    Tag = arrow
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
                Canvas.SetLeft(ScoreLabel, GameAreaWidth - 150);
                Canvas.SetTop(ScoreLabel, 10);
                ScoreLabel.FontSize = 16;
                ScoreLabel.Foreground = Brushes.White;
                Canvas.SetZIndex(ScoreLabel, 50);
            });
        }
    }
}