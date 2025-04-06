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
    // Defines properties for different enemy types
    public class EnemyType
    {
        public string[] EnemyImageFrames { get; set; }
        public string[] ProjectileImageFrames { get; set; }
        public double ProjectileSpeed { get; set; }
        public int Points { get; set; }
        public int Damage { get; set; } // ADDED: Damage dealt by projectile
    };

    // Manages enemy collective behavior, movement, shooting
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
        private const double BaseFireIntervalMin = 1.5; // Base minimum seconds between shots
        private const double BaseFireIntervalMax = 4.0; // Base maximum seconds between shots
        private const int MoveIntervalReductionPerLevelMs = 25; // How much move interval decreases per level
        private const int FireIntervalReductionPerLevelMs = 50; // How much fire interval decreases per level (example, faster than move)
        private const int MinFireIntervalMinMs = 400; // Absolute minimum for the faster firing interval
        private const int MinFireIntervalMaxMs = 800; // Absolute minimum for the slower firing interval
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

        // Enemy type definitions (index 0 = bottom row type)
        private EnemyType[] enemyTypes = new EnemyType[] {
            // Type 0 (Basic enemy)
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/archer/normal/animation1.png", "/Resources/archer/normal/animation2.png", "/Resources/archer/normal/animation3.png", "/Resources/archer/normal/animation4.png" },
                ProjectileImageFrames = new string[] { "/Resources/archer/bullet/bullet1.png" },
                ProjectileSpeed = 4.0, Points = 10, Damage = 1 // ADDED Damage
            },
            // Type 1 (Slightly stronger basic)
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/archer/normal/animation1.png", "/Resources/archer/normal/animation2.png", "/Resources/archer/normal/animation3.png", "/Resources/archer/normal/animation4.png"  },
                ProjectileImageFrames = new string[] { "/Resources/archer/bullet/bullet1.png" },
                ProjectileSpeed = 4.5, Points = 15, Damage = 1 // ADDED Damage
            },
            // Type 2 (Wizard - More Damage) C:\Users\dejwc\Downloads\space-invaders\space-invaders
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/mage/normal/animation1.png", "/Resources/mage/normal/animation2.png", "/Resources/mage/normal/animation3.png", "/Resources/mage/normal/animation3.png" },
                ProjectileImageFrames = new string[] { "/Resources/mage/bullet/bullet1.png", "/Resources/mage/bullet/bullet2.png", "/Resources/mage/bullet/bullet3.png", "/Resources/mage/bullet/bullet4.png" },
                ProjectileSpeed = 5.0, Points = 20, Damage = 2 // << WIZARD DAMAGE
            },
            // Type 3 (Stronger Wizard - More Damage)
            new EnemyType {
                 EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" },
                ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" },
                ProjectileSpeed = 5.5, Points = 25, Damage = 2 // << WIZARD DAMAGE
            },
        };

        public Enemies(Canvas gameCanvas, GameWindow gameWindow)
        {
            canvas = gameCanvas;
            this.gameWindow = gameWindow;
        }

        // Sets up a new wave of enemies
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

        // Calculates base timings based on current level
        private void CalculateBaseTimings()
        {
            // Movement Speed Scaling
            int moveReductionMs = (currentLevel - 1) * MoveIntervalReductionPerLevelMs; // No reduction for level 1
            currentBaseMoveIntervalMs = Math.Max(100, BaseMoveTickDurationMs - moveReductionMs); // Min 100ms interval

            // Firing Speed Scaling (using milliseconds for calculation)
            int fireReductionMs = (currentLevel - 1) * FireIntervalReductionPerLevelMs; // No reduction for level 1
            double baseFireIntervalMinMs = BaseFireIntervalMin * 1000;
            double baseFireIntervalMaxMs = BaseFireIntervalMax * 1000;

            double scaledFireMinMs = Math.Max(MinFireIntervalMinMs, baseFireIntervalMinMs - fireReductionMs);
            double scaledFireMaxMs = Math.Max(MinFireIntervalMaxMs, baseFireIntervalMaxMs - fireReductionMs);

            // Convert back to seconds for storage
            currentBaseFireIntervalMin = scaledFireMinMs / 1000.0;
            currentBaseFireIntervalMax = scaledFireMaxMs / 1000.0;

            // Ensure min interval is strictly less than max, even after scaling and capping
            if (currentBaseFireIntervalMin >= currentBaseFireIntervalMax)
            {
                // If min hits cap and max becomes equal or less, adjust max slightly
                currentBaseFireIntervalMax = currentBaseFireIntervalMin + 0.2; // Ensure max is always a bit higher than min
                // Or adjust min slightly lower if possible:
                // currentBaseFireIntervalMin = Math.Max(MinFireIntervalMinMs / 1000.0, currentBaseFireIntervalMax - 0.1);
            }

            Debug.WriteLine($"Level {currentLevel} Timings - Move Interval: {currentBaseMoveIntervalMs}ms, Fire Interval: {currentBaseFireIntervalMin:F2}s - {currentBaseFireIntervalMax:F2}s");
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
            // Ensure min < max after slowdown factor
            if (fireMin >= fireMax) fireMin = Math.Max(0.1, fireMax - 0.1); // Ensure min is slightly less than max, but not negative

            Debug.WriteLine($"Starting Shooting Timer - Interval: {fireMin:F2}s - {fireMax:F2}s (Slowed: {gameWindow.areEnemiesSlowed})");
            enemyFireTimer = new DispatcherTimer();
            // Handle potential case where fireMin and fireMax are extremely close or equal after capping/scaling
            if (fireMax <= fireMin)
            {
                enemyFireTimer.Interval = TimeSpan.FromSeconds(fireMin); // Use fixed min interval if max isn't greater
                Debug.WriteLine($"Warning: Fire Max interval ({fireMax:F2}s) not greater than Min interval ({fireMin:F2}s). Using fixed Min interval.");
            }
            else
            {
                enemyFireTimer.Interval = TimeSpan.FromSeconds(random.NextDouble() * (fireMax - fireMin) + fireMin);
            }
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
            double canvasWidth = 800;
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
                // Pass damage value to projectile constructor
                new EnemyProjectile(canvas, enemyX, enemyY, enemyType.ProjectileSpeed, enemyType.ProjectileImageFrames, enemyType.Damage, gameWindow);
                ResetFireTimerInterval();
            }
            else
            {
                Debug.WriteLine("Error: Could not find EnemyType data for the selected shooting enemy."); ResetFireTimerInterval();
            }
        }

        // Sets the interval for the next enemy shot based on current level/slowdown
        private void ResetFireTimerInterval()
        {
            if (enemyFireTimer == null) return;
            double fireMin = gameWindow.areEnemiesSlowed ? currentBaseFireIntervalMin * SlowdownFactor : currentBaseFireIntervalMin;
            double fireMax = gameWindow.areEnemiesSlowed ? currentBaseFireIntervalMax * SlowdownFactor : currentBaseFireIntervalMax;

            // Ensure min < max after potential slowdown/scaling
            if (fireMin >= fireMax) fireMin = Math.Max(0.1, fireMax - 0.1);

            // Handle potential case where fireMin and fireMax are extremely close or equal after capping/scaling
            if (fireMax <= fireMin)
            {
                enemyFireTimer.Interval = TimeSpan.FromSeconds(fireMin);
            }
            else
            {
                enemyFireTimer.Interval = TimeSpan.FromSeconds(random.NextDouble() * (fireMax - fireMin) + fireMin);
            }
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
                InitializeEnemies(); // Recalculates timings for new level
            }
        }
    } // End of Enemies class

    // ========================================================================

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
        private int damage; // ADDED: Store projectile damage
        private GameWindow gameWindow;

        // UPDATED Constructor to accept damage
        public EnemyProjectile(Canvas gameCanvas, double startX, double startY, double projectileSpeed, string[] projectileFrames, int projectileDamage, GameWindow window)
        {
            canvas = gameCanvas;
            speed = projectileSpeed;
            gameWindow = window;
            damage = projectileDamage; // Store damage

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

        // Checks for collisions with shields and player (considering armor)
        private void CheckCollision()
        {
            if (projectileImage == null || canvas == null || !canvas.Children.Contains(projectileImage) || gameWindow == null)
            {
                if (projectileImage == null || canvas == null) Cleanup(); return;
            }

            Rect projectileRect = new Rect(Canvas.GetLeft(projectileImage), Canvas.GetTop(projectileImage), projectileImage.Width, projectileImage.Height);

            // Check Shield Collisions
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

            // Check Player Collision
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
                        // Use stored damage value
                        player.SetHP(player.GetHP() - this.damage);
                        Debug.WriteLine($"Player hit! HP remaining: {player.GetHP()}, Damage Taken: {this.damage}");
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