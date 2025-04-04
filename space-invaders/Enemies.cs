using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;

namespace Space_intruders
{
    public class EnemyType
    {
        public string[] EnemyImageFrames { get; set; }
        public string[] ProjectileImageFrames { get; set; }
        public double ProjectileSpeed { get; set; }
        public int Points { get; set; }
    };

    public class Enemies
    {
        // --- Constants ---
        private const int EnemyWidth = 50;
        private const int EnemyHeight = 50;
        private const int Columns = 10;
        private const int Rows = 4;
        private const int EnemySpacing = 10;
        private const int BaseMoveDistance = 10;
        private const int BaseMoveDownDistance = 15;
        private const int BaseMoveTickDurationMs = 500;
        private const double BaseFireIntervalMin = 1.5;
        private const double BaseFireIntervalMax = 4.0;
        private const double SlowdownFactor = 2.0;
        private const double LoseThreshold = 490;

        // --- State Variables ---
        private int enemyFrameIndex = 0;
        private bool movingRight = true;
        public List<Image> enemies = new List<Image>();
        private Dictionary<Image, EnemyType> enemyData = new Dictionary<Image, EnemyType>();
        private DispatcherTimer enemyMoveTimer;
        private DispatcherTimer enemyFireTimer;
        private DispatcherTimer enemyAnimationTimer;
        private Canvas canvas;
        private Random random = new Random();
        public GameWindow gameWindow;
        private int currentLevel = 1;
        private bool waveCleared = false;
        private int currentBaseMoveIntervalMs;
        private double currentBaseFireIntervalMin;
        private double currentBaseFireIntervalMax;

        private EnemyType[] enemyTypes = new EnemyType[] {
            new EnemyType { EnemyImageFrames = new string[] { "/Resources/enemy1.png", "/Resources/enemy2.png", "/Resources/enemy1.png", "/Resources/enemy2.png" }, ProjectileImageFrames = new string[] { "/Resources/ball.png" }, ProjectileSpeed = 4.0, Points = 10 },
            new EnemyType { EnemyImageFrames = new string[] { "/Resources/enemy2.png", "/Resources/enemy1.png", "/Resources/enemy1.png", "/Resources/enemy2.png"  }, ProjectileImageFrames = new string[] { "/Resources/ball.png" }, ProjectileSpeed = 4.5, Points = 15 },
            new EnemyType { EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" }, ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" }, ProjectileSpeed = 5.0, Points = 20 },
            new EnemyType { EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" }, ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" }, ProjectileSpeed = 5.5, Points = 25 },
        };

        public Enemies(Canvas gameCanvas, GameWindow gameWindow)
        {
            canvas = gameCanvas;
            this.gameWindow = gameWindow;
        }

        public void InitializeEnemies()
        {
            waveCleared = false;
            foreach (var enemyImg in enemies)
            {
                if (canvas != null && canvas.Children.Contains(enemyImg))
                    canvas.Children.Remove(enemyImg);
            }
            enemies.Clear();
            enemyData.Clear();

            int rowsToSpawn = Math.Min(Rows, enemyTypes.Length);
            int colsToSpawn = Columns;
            double totalBlockWidth = colsToSpawn * EnemyWidth + (colsToSpawn - 1) * EnemySpacing;
            double canvasWidth = 800; // Use design width
            double startX = (canvasWidth - totalBlockWidth) / 2;
            if (startX < 0) startX = 5;
            double startY = 50;

            for (int row = 0; row < rowsToSpawn; row++)
            {
                int typeIndex = (rowsToSpawn - 1) - row;
                if (typeIndex < 0 || typeIndex >= enemyTypes.Length)
                {
                    Debug.WriteLine($"Warning: Invalid typeIndex {typeIndex} for screen row {row}. Defaulting to 0.");
                    typeIndex = 0;
                }
                EnemyType enemyType = enemyTypes[typeIndex];

                for (int col = 0; col < colsToSpawn; col++)
                {
                    Image enemy = new Image
                    {
                        Width = EnemyWidth,
                        Height = EnemyHeight,
                        Source = LoadEnemyImage(enemyType.EnemyImageFrames, 0),
                        Tag = enemyType
                    };
                    Canvas.SetLeft(enemy, startX + col * (EnemyWidth + EnemySpacing));
                    Canvas.SetTop(enemy, startY + row * (EnemyHeight + EnemySpacing));
                    canvas.Children.Add(enemy);
                    enemies.Add(enemy);
                    enemyData.Add(enemy, enemyType);
                }
            }
            Debug.WriteLine($"Level {currentLevel} initialized with {enemies.Count} enemies across {rowsToSpawn} rows.");

            CalculateBaseTimings();
            StartEnemyMovement();
            StartEnemyShooting();
            StartEnemyAnimation();
        }

        private BitmapImage LoadEnemyImage(string[] frames, int frameIndex)
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.WriteLine("Error: Attempted to load image from null or empty frames array."); return null;
            }
            int index = frameIndex % frames.Length;
            try
            {
                return new BitmapImage(new Uri(frames[index], UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load enemy image frame {frames[index]}: {ex.Message}"); return null;
            }
        }

        private void CalculateBaseTimings()
        {
            currentBaseMoveIntervalMs = Math.Max(100, BaseMoveTickDurationMs - (currentLevel * 25));
            currentBaseFireIntervalMin = Math.Max(0.5, BaseFireIntervalMin - (currentLevel * 0.1));
            currentBaseFireIntervalMax = Math.Max(1.0, BaseFireIntervalMax - (currentLevel * 0.2));
            if (currentBaseFireIntervalMin >= currentBaseFireIntervalMax)
                currentBaseFireIntervalMin = currentBaseFireIntervalMax - 0.1;
            Debug.WriteLine($"Level {currentLevel} Base Timings - Move: {currentBaseMoveIntervalMs}ms, Fire: {currentBaseFireIntervalMin:F1}s - {currentBaseFireIntervalMax:F1}s");
        }

        // --- Timer Management ---
        private void StartEnemyMovement()
        {
            StopTimer(enemyMoveTimer);
            int moveInterval = gameWindow.areEnemiesSlowed ? (int)(currentBaseMoveIntervalMs * SlowdownFactor) : currentBaseMoveIntervalMs;
            Debug.WriteLine($"Starting Movement Timer - Interval: {moveInterval}ms (Slowed: {gameWindow.areEnemiesSlowed})");
            enemyMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(moveInterval) };
            enemyMoveTimer.Tick += MoveEnemies;
            enemyMoveTimer.Start();
        }

        private void StartEnemyShooting()
        {
            StopTimer(enemyFireTimer);
            double fireMin = gameWindow.areEnemiesSlowed ? currentBaseFireIntervalMin * SlowdownFactor : currentBaseFireIntervalMin;
            double fireMax = gameWindow.areEnemiesSlowed ? currentBaseFireIntervalMax * SlowdownFactor : currentBaseFireIntervalMax;
            if (fireMin >= fireMax) fireMin = fireMax - 0.1;
            Debug.WriteLine($"Starting Shooting Timer - Interval: {fireMin:F1}s - {fireMax:F1}s (Slowed: {gameWindow.areEnemiesSlowed})");
            enemyFireTimer = new DispatcherTimer();
            enemyFireTimer.Interval = TimeSpan.FromSeconds(random.NextDouble() * (fireMax - fireMin) + fireMin);
            enemyFireTimer.Tick += EnemyShoot;
            enemyFireTimer.Start();
        }

        private void StartEnemyAnimation()
        {
            StopTimer(enemyAnimationTimer);
            enemyAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(BaseMoveTickDurationMs / 2.0) };
            enemyAnimationTimer.Tick += AnimateEnemies;
            enemyAnimationTimer.Start();
        }

        private void StopTimer(DispatcherTimer timer) { timer?.Stop(); }

        public void StopTimers()
        {
            StopTimer(enemyMoveTimer);
            StopTimer(enemyFireTimer);
            StopTimer(enemyAnimationTimer);
            Debug.WriteLine("All enemy timers stopped.");
        }

        // --- Boost Control Methods ---
        public void SlowDownEnemies()
        {
            Debug.WriteLine("Enemies: Received SlowDown command.");
            StartEnemyMovement();
            StartEnemyShooting();
        }

        public void SpeedUpEnemies()
        {
            Debug.WriteLine("Enemies: Received SpeedUp command.");
            StartEnemyMovement();
            StartEnemyShooting();
        }

        // --- Game Logic Methods ---
        private void MoveEnemies(object sender, EventArgs e)
        {
            if (enemies.Count == 0 || waveCleared) { StopTimer(enemyMoveTimer); return; }

            double leftMost = double.MaxValue, rightMost = double.MinValue, bottomMost = double.MinValue;
            foreach (var enemy in enemies.ToList())
            {
                if (canvas == null || !canvas.Children.Contains(enemy)) { enemies.Remove(enemy); enemyData.Remove(enemy); continue; }
                double currentLeft = Canvas.GetLeft(enemy); double currentTop = Canvas.GetTop(enemy);
                leftMost = Math.Min(leftMost, currentLeft);
                rightMost = Math.Max(rightMost, currentLeft + EnemyWidth);
                bottomMost = Math.Max(bottomMost, currentTop + EnemyHeight);
            }

            if (bottomMost >= LoseThreshold)
            {
                Debug.WriteLine($"Invaders reached lose threshold ({bottomMost:F0} >= {LoseThreshold}). Game Over.");
                StopTimers(); gameWindow?.GameOver(false); return;
            }

            bool moveDown = false;
            int moveDistance = BaseMoveDistance;
            double canvasWidth = 800; // Use design width
            if (movingRight && rightMost + moveDistance >= canvasWidth) { movingRight = false; moveDown = true; }
            else if (!movingRight && leftMost - moveDistance <= 0) { movingRight = true; moveDown = true; }

            foreach (var enemy in enemies)
            {
                if (canvas == null || !canvas.Children.Contains(enemy)) continue;
                if (moveDown) { Canvas.SetTop(enemy, Canvas.GetTop(enemy) + BaseMoveDownDistance); }
                else { Canvas.SetLeft(enemy, Canvas.GetLeft(enemy) + (movingRight ? moveDistance : -moveDistance)); }
            }
        }

        private void AnimateEnemies(object sender, EventArgs e)
        {
            enemyFrameIndex = (enemyFrameIndex + 1) % 4;
            foreach (var enemy in enemies)
            {
                if (enemyData.TryGetValue(enemy, out EnemyType enemyType))
                {
                    if (enemyType?.EnemyImageFrames != null && enemyType.EnemyImageFrames.Length > 0)
                    {
                        int frameCount = enemyType.EnemyImageFrames.Length;
                        int currentFrameIndexForThisType = enemyFrameIndex % frameCount;
                        enemy.Source = LoadEnemyImage(enemyType.EnemyImageFrames, currentFrameIndexForThisType);
                    }
                }
            }
        }

        private void EnemyShoot(object sender, EventArgs e)
        {
            if (enemies.Count == 0 || waveCleared || enemyFireTimer == null || canvas == null) return;
            int shootingEnemyIndex = random.Next(enemies.Count);
            Image shootingEnemy = enemies[shootingEnemyIndex];

            if (canvas == null || !canvas.Children.Contains(shootingEnemy) || !enemyData.ContainsKey(shootingEnemy))
            {
                Debug.WriteLine("Skipping shot: Selected enemy no longer valid."); ResetFireTimerInterval(); return;
            }

            if (enemyData.TryGetValue(shootingEnemy, out EnemyType enemyType))
            {
                double enemyX = Canvas.GetLeft(shootingEnemy) + (EnemyWidth / 2.0) - 10;
                double enemyY = Canvas.GetTop(shootingEnemy) + EnemyHeight;
                new EnemyProjectile(canvas, enemyX, enemyY, enemyType.ProjectileSpeed, enemyType.ProjectileImageFrames, gameWindow);
                ResetFireTimerInterval();
            }
            else
            {
                Debug.WriteLine("Error: Could not find EnemyType data for the selected shooting enemy."); ResetFireTimerInterval();
            }
        }

        private void ResetFireTimerInterval()
        {
            if (enemyFireTimer == null) return;
            double fireMinBase = currentBaseFireIntervalMin; double fireMaxBase = currentBaseFireIntervalMax;
            double fireMin = gameWindow.areEnemiesSlowed ? fireMinBase * SlowdownFactor : fireMinBase;
            double fireMax = gameWindow.areEnemiesSlowed ? fireMaxBase * SlowdownFactor : fireMaxBase;
            if (fireMin >= fireMax) fireMin = fireMax - 0.1;
            enemyFireTimer.Interval = TimeSpan.FromSeconds(random.NextDouble() * (fireMax - fireMin) + fireMin);
        }

        public void EnemyHit(Image enemy)
        {
            if (enemy != null && enemies.Contains(enemy) && canvas != null)
            {
                double enemyX = Canvas.GetLeft(enemy) + enemy.Width / 2.0;
                double enemyY = Canvas.GetTop(enemy) + enemy.Height / 2.0;

                if (enemyData.TryGetValue(enemy, out EnemyType type)) gameWindow?.AddScore(type.Points);

                enemies.Remove(enemy);
                enemyData.Remove(enemy);
                if (canvas.Children.Contains(enemy)) canvas.Children.Remove(enemy);

                Debug.WriteLine($"EnemyHit: Enemy destroyed at ({enemyX:F0}, {enemyY:F0}). Attempting to call TrySpawnBoost.");
                gameWindow?.TrySpawnBoost(enemyX, enemyY);

                CheckWaveCleared();
            }
            else if (canvas == null)
            {
                Debug.WriteLine("EnemyHit: Cannot process hit, canvas is null.");
            }
        }

        private void CheckWaveCleared()
        {
            if (!waveCleared && enemies.Count == 0 && canvas != null)
            {
                waveCleared = true; Debug.WriteLine($"Wave {currentLevel} cleared!");
                StopTimers();
                currentLevel++; Debug.WriteLine($"--- Starting Level {currentLevel} ---");
                InitializeEnemies();
            }
        }
    }

    // Represents a projectile fired by an enemy
    public class EnemyProjectile
    {
        private Image projectileImage;
        private DispatcherTimer moveTimer;
        private DispatcherTimer animationTimer;
        private double speed;
        private Canvas canvas;
        private string[] frames;
        private int frameIndex = 0;
        private GameWindow gameWindow;

        public EnemyProjectile(Canvas gameCanvas, double startX, double startY, double projectileSpeed, string[] projectileFrames, GameWindow window)
        {
            canvas = gameCanvas; speed = projectileSpeed; gameWindow = window;
            frames = projectileFrames ?? new string[] { "/Resources/ball.png" };
            if (frames.Length == 0) frames = new string[] { "/Resources/ball.png" };

            projectileImage = new Image { Width = 20, Height = 20, Source = LoadProjectileImage(0) };
            if (projectileImage.Source == null) { Debug.WriteLine("Error loading projectile image."); return; }

            if (canvas == null)
            {
                Debug.WriteLine("Error: Cannot add projectile, canvas is null."); return;
            }

            Canvas.SetLeft(projectileImage, startX); Canvas.SetTop(projectileImage, startY);
            canvas.Children.Add(projectileImage); Canvas.SetZIndex(projectileImage, 1);
            projectileImage.Tag = this;

            moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            moveTimer.Tick += MoveProjectile; moveTimer.Start();

            if (frames.Length > 1)
            {
                animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                animationTimer.Tick += AnimateProjectile; animationTimer.Start();
            }
        }

        private BitmapImage LoadProjectileImage(int frameIndex)
        {
            int index = frameIndex % frames.Length;
            try { return new BitmapImage(new Uri(frames[index], UriKind.Relative)); }
            catch (Exception ex) { Debug.WriteLine($"Failed to load projectile frame {frames[index]}: {ex.Message}"); return null; }
        }

        private void MoveProjectile(object sender, EventArgs e)
        {
            if (projectileImage == null || canvas == null || !canvas.Children.Contains(projectileImage)) { Cleanup(); return; }
            double currentTop = Canvas.GetTop(projectileImage);
            double canvasHeight = 580; // Use design height
            if (currentTop >= canvasHeight) { Cleanup(); }
            else { Canvas.SetTop(projectileImage, currentTop + speed); CheckCollision(); }
        }

        private void AnimateProjectile(object sender, EventArgs e)
        {
            if (projectileImage == null || frames == null || frames.Length <= 1 || animationTimer == null || !animationTimer.IsEnabled)
            {
                animationTimer?.Stop(); return;
            }
            frameIndex = (frameIndex + 1) % frames.Length;
            BitmapImage nextFrame = LoadProjectileImage(frameIndex);
            if (nextFrame == null) { Debug.WriteLine("Projectile frame load failed during animation."); Cleanup(); }
            else { projectileImage.Source = nextFrame; }
        }

        private void CheckCollision()
        {
            if (projectileImage == null || canvas == null || !canvas.Children.Contains(projectileImage) || gameWindow == null)
            {
                if (projectileImage == null || canvas == null) Cleanup(); return;
            }

            Rect projectileRect = new Rect(Canvas.GetLeft(projectileImage), Canvas.GetTop(projectileImage), projectileImage.Width, projectileImage.Height);

            foreach (var shield in gameWindow.shields.ToList())
            {
                if (shield.GetDurability() <= 0) continue;
                Image shieldImg = shield.GetImage();
                if (shieldImg == null || canvas == null || !canvas.Children.Contains(shieldImg)) continue;
                Rect shieldRect = new Rect(Canvas.GetLeft(shieldImg), Canvas.GetTop(shieldImg), shieldImg.Width, shieldImg.Height);
                if (projectileRect.IntersectsWith(shieldRect))
                {
                    shield.TakeDamage(); Cleanup(); return;
                }
            }

            Image playerImage = gameWindow.playerImage;
            if (playerImage != null && canvas != null && canvas.Children.Contains(playerImage))
            {
                Rect playerRect = new Rect(Canvas.GetLeft(playerImage), Canvas.GetTop(playerImage), playerImage.Width, playerImage.Height);
                if (projectileRect.IntersectsWith(playerRect))
                {
                    if (gameWindow.isArmorActive)
                    {
                        Debug.WriteLine("Player hit, but Armor absorbed the hit!");
                    }
                    else
                    {
                        Player player = gameWindow.player;
                        player.SetHP(player.GetHP() - 1);
                        Debug.WriteLine($"Player hit! HP remaining: {player.GetHP()}");
                        gameWindow.UpdateHeartDisplay();
                        if (player.GetHP() <= 0)
                        {
                            Debug.WriteLine("Player HP reached 0. Triggering Game Over.");
                            if (gameWindow != null) gameWindow.GameOver(false);
                        }
                    }
                    Cleanup(); return;
                }
            }
        }

        private void Cleanup()
        {
            moveTimer?.Stop(); animationTimer?.Stop();
            moveTimer = null; animationTimer = null;
            if (projectileImage != null && canvas != null && canvas.Children.Contains(projectileImage))
            {
                canvas.Children.Remove(projectileImage);
            }
            projectileImage = null; gameWindow = null; canvas = null;
        }
    }
}