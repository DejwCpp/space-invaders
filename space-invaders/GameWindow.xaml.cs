using System;
using System.Collections.Generic;
using System.IO; 
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
using System.Text; 

namespace Space_intruders
{
    public partial class GameWindow : Window
    {
        // --- Static Members for Score File Path ---
        private static readonly string ScoreFileName = "scores.csv";
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpaceIntrudersMedieval");
        private static readonly string ScoreFilePath = Path.Combine(AppDataFolder, ScoreFileName);

        // --- Game State Variables ---
        public Player player = new();
        static int counter = 0;
        public Dictionary<int, Arrow> arrows = new Dictionary<int, Arrow>();
        public List<Shield> shields = new List<Shield>();
        public Enemies enemies { get; private set; }
        bool canShoot = true;
        private int currentScore = 0;
        private Random random = new Random();

        // --- Input State ---
        private bool isMovingLeft = false;
        private bool isMovingRight = false;
        private bool isShootingKeyDown = false;

        // --- Timers ---
        private DispatcherTimer gameLoopTimer;
        private DispatcherTimer fasterShootingTimer;
        private DispatcherTimer armorTimer;
        private DispatcherTimer slowEnemiesTimer;

        // --- Boosts ---
        private List<Boost> activeBoostsOnScreen = new List<Boost>();
        private const double BoostSpawnChance = 0.2;
        private const int MaxPlayerHp = 3;
        private int baseShootCooldownMs = 500;
        private int currentShootCooldownMs;
        private const int FasterShootingDurationSeconds = 5;
        private const int FasterShootCooldownMs = 150;
        public bool isArmorActive { get; private set; } = false;
        private const int ArmorDurationSeconds = 3;
        private Image armorIndicator;
        public bool areEnemiesSlowed { get; private set; } = false;
        private const int SlowEnemiesDurationSeconds = 6;

        // --- Constants ---
        private const double GameAreaWidth = 800;
        private const double GameAreaHeight = 580;

        // --- Game State ---
        private bool isGameOver = false;

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
            player.SetSpeed(5);
            player.SetArmour(1);
            player.SetDMG(1);
        }

        private void InitializeBoostTimers()
        {
            fasterShootingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(FasterShootingDurationSeconds) };
            fasterShootingTimer.Tick += FasterShootingTimer_Tick;

            armorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ArmorDurationSeconds) };
            armorTimer.Tick += ArmorTimer_Tick;

            slowEnemiesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(SlowEnemiesDurationSeconds) };
            slowEnemiesTimer.Tick += SlowEnemiesTimer_Tick;
        }

        private void InitializeGameLoop()
        {
            gameLoopTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            gameLoopTimer.Tick += GameLoopTick;
            gameLoopTimer.Start();
        }

        // --- Main Game Loop ---
        private void GameLoopTick(object sender, EventArgs e)
        {
            if (isGameOver) return;
            MovePlayer();
            TryShoot();
        }

        // --- Input Event Handlers ---
        private void GameWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left: case Key.A: isMovingLeft = true; break;
                case Key.Right: case Key.D: isMovingRight = true; break;
                case Key.Space: isShootingKeyDown = true; break;
            }
        }

        private void GameWindow_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left: case Key.A: isMovingLeft = false; break;
                case Key.Right: case Key.D: isMovingRight = false; break;
                case Key.Space: isShootingKeyDown = false; break;
            }
        }

        // --- Player Movement Logic ---
        private void MovePlayer()
        {
            if (playerImage == null || isGameOver) return;

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

        // --- Shooting Logic ---
        private void TryShoot()
        {
            if (isGameOver) return;
            if (isShootingKeyDown && canShoot)
            {
                canShoot = false;
                SpawnNewArrow();
                StartShootCooldown();
            }
        }

        private async void StartShootCooldown()
        {
            await Task.Delay(currentShootCooldownMs);
            if (!isGameOver)
            {
                canShoot = true;
            }
        }

        // --- Boost Spawning ---
        public void TrySpawnBoost(double x, double y)
        {
            if (isGameOver) return;
            if (random.NextDouble() < BoostSpawnChance)
            {
                Debug.WriteLine($"TrySpawnBoost: Chance met...");
                Array boostTypes = Enum.GetValues(typeof(Boost.BoostType));
                Boost.BoostType randomType = (Boost.BoostType)boostTypes.GetValue(random.Next(boostTypes.Length));
                Debug.WriteLine($"TrySpawnBoost: Determined type: {randomType}...");

                Boost newBoost = new Boost(gameCanvas, x, y, randomType, this);
                if (newBoost.BoostImage != null)
                {
                    activeBoostsOnScreen.Add(newBoost);
                    Debug.WriteLine($"TrySpawnBoost: Successfully created boost: {randomType}.");
                }
                else
                {
                    Debug.WriteLine($"TrySpawnBoost: Failed to create Boost object for type {randomType}.");
                }
            }
        }

        // --- Boost Activation ---
        public void ActivateBoost(Boost.BoostType type)
        {
            if (isGameOver) return;
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
            Debug.WriteLine("Faster shooting expired.");
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
                        catch (Exception ex) { Debug.WriteLine($"Error loading shield indicator: {ex.Message}"); armorIndicator = null; }
                    }
                    if (armorIndicator != null) { armorIndicator.Visibility = Visibility.Visible; UpdateArmorIndicatorPosition(); }
                }
                else { if (armorIndicator != null) { armorIndicator.Visibility = Visibility.Hidden; } }
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
            if (!areEnemiesSlowed) { areEnemiesSlowed = true; enemies?.SlowDownEnemies(); }
            slowEnemiesTimer.Stop();
            slowEnemiesTimer.Start();
            Debug.WriteLine("Enemies slowed!");
        }

        private void SlowEnemiesTimer_Tick(object sender, EventArgs e)
        {
            slowEnemiesTimer.Stop();
            if (areEnemiesSlowed) { areEnemiesSlowed = false; enemies?.SpeedUpEnemies(); Debug.WriteLine("Enemy slow expired."); }
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

        // --- Game Over Handling --- MODIFIED ORDER
        public void GameOver(bool won = false)
        {
            if (isGameOver) return;
            isGameOver = true;

            Debug.WriteLine($"Game Over called. Won: {won}");

            // Stop Input and Timers first
            this.KeyDown -= GameWindow_KeyDown;
            this.KeyUp -= GameWindow_KeyUp;
            gameLoopTimer?.Stop();
            fasterShootingTimer?.Stop();
            armorTimer?.Stop();
            slowEnemiesTimer?.Stop();
            enemies?.StopTimers();
            foreach (var arrow in arrows.Values.ToList()) arrow.StopTimer();
            arrows.Clear();
            foreach (var boost in activeBoostsOnScreen.ToList()) boost.Cleanup();
            activeBoostsOnScreen.Clear();


            // ** THEN: Update UI to show Game Over screen **
            Dispatcher.Invoke(() => {

                // --- Hide all game elements ---
                gameCanvas.Children.Clear();
                if (playerImage != null) playerImage.Visibility = Visibility.Collapsed;
                if (armorIndicator != null) armorIndicator.Visibility = Visibility.Collapsed;
                if (heartsPanel != null) heartsPanel.Visibility = Visibility.Collapsed;
                if (ScoreLabel != null) ScoreLabel.Visibility = Visibility.Collapsed;
                foreach (var shield in shields)
                {
                    Image img = shield.GetImage();
                    if (img != null) img.Visibility = Visibility.Collapsed;
                }

                // --- Display Game Over Text ---
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
                Canvas.SetTop(gameOverLabel, (GameAreaHeight - labelHeightEstimate) / 2 - 50);
                Canvas.SetZIndex(gameOverLabel, 100);
                gameCanvas.Children.Add(gameOverLabel);


                // --- Display Return to Menu Button ---
                Button returnButton = new Button
                {
                    Content = "Return to Menu",
                    Width = 180,
                    Height = 40,
                    FontSize = 16,
                    Background = Brushes.DarkGoldenrod,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(returnButton, (GameAreaWidth - returnButton.Width) / 2);
                Canvas.SetTop(returnButton, Canvas.GetTop(gameOverLabel) + labelHeightEstimate + 20);
                Canvas.SetZIndex(returnButton, 100);
                returnButton.Click += ReturnToMenu_Click;
                gameCanvas.Children.Add(returnButton);
            });

            // ** FINALLY: Prompt for Nickname and Save Score if applicable **
            // This happens after the Game Over screen is displayed
            if (!won && currentScore > 0)
            {
                // No need for Dispatcher here as PromptAndSaveScore uses ShowDialog
                PromptAndSaveScore();
            }
        }

        // --- Event Handler for Return Button ---
        private void ReturnToMenu_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }


        // --- Score Saving ---
        private void PromptAndSaveScore()
        {
            NicknameInputDialog dialog = new NicknameInputDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string nickname = dialog.Nickname;
                SaveScore(nickname, currentScore);
            }
        }

        private void SaveScore(string nickname, int score)
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);
                ScoreEntry entry = new ScoreEntry(nickname, score, DateTime.UtcNow);
                string scoreLine = $"{entry.Nickname},{entry.Score},{entry.Timestamp:o}";
                File.AppendAllText(ScoreFilePath, scoreLine + Environment.NewLine, Encoding.UTF8);
                Debug.WriteLine($"Score saved: {scoreLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving score: {ex.Message}");
                MessageBox.Show($"Could not save score.\nError: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --- Arrow Handling ---
        public void SpawnNewArrow()
        {
            if (isGameOver) return;
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
                { /* ... properties ... */
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
                    if (arrows.ContainsKey(arrowID))
                        arrows.Remove(arrowID);
                }
            });
        }

        // --- Shield & Score Handling ---
        private void InitializeShields()
        {
            Dispatcher.Invoke(() => {
                foreach (var existingShield in shields)
                {
                    Image img = existingShield.GetImage();
                    if (img != null && gameCanvas.Children.Contains(img))
                    {
                        gameCanvas.Children.Remove(img);
                    }
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
            if (isGameOver) return;
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