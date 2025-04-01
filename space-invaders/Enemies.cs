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
        // Constants
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

        // State Variables
        private int enemyFrameIndex = 0;
        private bool movingRight = true;
        public List<Image> enemies = new List<Image>();
        private Dictionary<Image, EnemyType> enemyData = new Dictionary<Image, EnemyType>();
        private DispatcherTimer enemyMoveTimer;
        private DispatcherTimer enemyFireTimer;
        private DispatcherTimer enemyAnimationTimer;
        private Canvas canvas;
        private Random random = new Random();
        private double loseThreshold = 490; 
        public GameWindow gameWindow;       
        private int currentLevel = 1;      
        private bool waveCleared = false;  

        // Enemy Definitions (Order: Weaker/Bottom Rows first, Stronger/Top Rows last) 
        private EnemyType[] enemyTypes = new EnemyType[]
        {
            // Type 0 (Intended for Lowest Row)
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/enemy1.png", "/Resources/enemy2.png", "/Resources/enemy1.png", "/Resources/enemy2.png" },
                ProjectileImageFrames = new string[] { "/Resources/ball.png" },
                ProjectileSpeed = 4.0, Points = 10
            },
            // Type 1
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/enemy2.png", "/Resources/enemy1.png", "/Resources/enemy1.png", "/Resources/enemy2.png"  },
                ProjectileImageFrames = new string[] { "/Resources/ball.png" },
                ProjectileSpeed = 4.5, Points = 15 
            },
            // Type 2
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" },
                ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" },
                ProjectileSpeed = 5.0, Points = 20
            },
            // Type 3 (Intended for Highest Row)
            new EnemyType {
                 EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" }, 
                ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" }, //
                ProjectileSpeed = 5.5, Points = 25 
            },
        };

        public Enemies(Canvas gameCanvas, GameWindow gameWindow)
        {
            canvas = gameCanvas;
            this.gameWindow = gameWindow;
        }

        public void InitializeEnemies()
        {
            waveCleared = false; 
            enemies.Clear();     
            enemyData.Clear();  

            // Determine how many rows to actually spawn for this level
            int rowsToSpawn = Math.Min(Rows, enemyTypes.Length);
            int colsToSpawn = Columns; 

            double totalBlockWidth = colsToSpawn * EnemyWidth + (colsToSpawn - 1) * EnemySpacing;
          
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 800;
            double startX = (canvasWidth - totalBlockWidth) / 2;
            if (startX < 0) startX = 5; 
            double startY = 50; 

            // Loop through the rows as they appear on the screen (0 = Top Row)
            for (int row = 0; row < rowsToSpawn; row++)
            {
                // Calculate the correct EnemyType index based on the row position from the BOTTOM.
                int typeIndex = (rowsToSpawn - 1) - row;

                if (typeIndex < 0 || typeIndex >= enemyTypes.Length)
                {
                    Debug.WriteLine($"Warning: Calculated invalid typeIndex {typeIndex} for screen row {row}. Defaulting to 0.");
                    typeIndex = 0; 
                }

                EnemyType enemyType = enemyTypes[typeIndex];

                // Spawn enemies in the current row
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

                    // Add to canvas and internal tracking lists
                    canvas.Children.Add(enemy);
                    enemies.Add(enemy);
                    enemyData.Add(enemy, enemyType); 
                }
            }

            Debug.WriteLine($"Level {currentLevel} initialized with {enemies.Count} enemies across {rowsToSpawn} rows.");

            // Start or restart timers for the new wave with potentially updated speeds/intervals
            StartEnemyMovement();
            StartEnemyShooting();
            StartEnemyAnimation();
        }

        // Helper for safe image loading
        private BitmapImage LoadEnemyImage(string[] frames, int frameIndex)
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.WriteLine("Error: Attempted to load image from null or empty frames array.");
                return null; 
            }
            int index = frameIndex % frames.Length; 

            try
            {
                return new BitmapImage(new Uri(frames[index], UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load enemy image frame {frames[index]}: {ex.Message}");
                return null; 
            }
        }

        private void StartEnemyMovement()
        {
            StopTimer(enemyMoveTimer);

            // Increase speed based on level (decrease interval)
            int moveInterval = Math.Max(100, BaseMoveTickDurationMs - (currentLevel * 25)); // Faster per level, min 100ms
            Debug.WriteLine($"Level {currentLevel} move interval: {moveInterval}ms");

            enemyMoveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(moveInterval)
            };
            enemyMoveTimer.Tick += MoveEnemies;
            enemyMoveTimer.Start();
        }

        private void StartEnemyShooting()
        {
            StopTimer(enemyFireTimer);

            // Increase firing rate based on level (decrease interval)
            double fireMin = Math.Max(0.5, BaseFireIntervalMin - (currentLevel * 0.1));
            double fireMax = Math.Max(1.0, BaseFireIntervalMax - (currentLevel * 0.2)); 
            if (fireMin >= fireMax) fireMin = fireMax - 0.1; 
            Debug.WriteLine($"Level {currentLevel} fire interval: {fireMin:F1}s - {fireMax:F1}s");

            enemyFireTimer = new DispatcherTimer();
            // Set initial random interval
            enemyFireTimer.Interval = TimeSpan.FromSeconds(random.NextDouble() * (fireMax - fireMin) + fireMin);
            enemyFireTimer.Tick += EnemyShoot;
            enemyFireTimer.Start();
        }

        private void StartEnemyAnimation()
        {
            StopTimer(enemyAnimationTimer); 

            enemyAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(BaseMoveTickDurationMs / 2.0)
            };
            enemyAnimationTimer.Tick += AnimateEnemies;
            enemyAnimationTimer.Start();
        }

        private void StopTimer(DispatcherTimer timer)
        {
            if (timer != null)
                timer.Stop();
        }

        public void StopTimers()
        {
            StopTimer(enemyMoveTimer);
            StopTimer(enemyFireTimer);
            StopTimer(enemyAnimationTimer);
            Debug.WriteLine("All enemy timers stopped.");
        }

        private void MoveEnemies(object sender, EventArgs e)
        {
            // If no enemies left or wave is clearing, stop movement
            if (enemies.Count == 0 || waveCleared)
            {
                StopTimer(enemyMoveTimer); 
                return;
            }

            double leftMost = double.MaxValue;
            double rightMost = double.MinValue;
            double bottomMost = double.MinValue; 

            
            foreach (var enemy in enemies.ToList()) 
            {
                // Check if the enemy still exists on the canvas (might have been removed by a hit)
                if (!canvas.Children.Contains(enemy))
                {
                    enemies.Remove(enemy);
                    enemyData.Remove(enemy);
                    continue;
                }

                double currentLeft = Canvas.GetLeft(enemy);
                double currentTop = Canvas.GetTop(enemy);
                leftMost = Math.Min(leftMost, currentLeft);
                rightMost = Math.Max(rightMost, currentLeft + EnemyWidth); 
                bottomMost = Math.Max(bottomMost, currentTop + EnemyHeight); 
            }

            // Check if any enemy reached the lose threshold
            if (bottomMost >= loseThreshold)
            {
                Debug.WriteLine($"Invaders reached lose threshold ({bottomMost} >= {loseThreshold}). Game Over.");
                StopTimers();               
                gameWindow?.GameOver(false); 
                return;                     
            }

            // Determine direction and if a downward move is needed
            bool moveDown = false;
            int moveDistance = BaseMoveDistance + (currentLevel / 2);
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 800; 

            if (movingRight && rightMost + moveDistance >= canvasWidth)
            {
                movingRight = false; 
                moveDown = true;    
            }
            else if (!movingRight && leftMost - moveDistance <= 0)
            {
                movingRight = true; 
                moveDown = true;    
            }

            foreach (var enemy in enemies) 
            {
                if (moveDown)
                {
                    // Move down
                    double newTop = Canvas.GetTop(enemy) + BaseMoveDownDistance; // Use constant downward step
                    Canvas.SetTop(enemy, newTop);
                }
                else
                {
                    // Move horizontally
                    double newLeft = Canvas.GetLeft(enemy) + (movingRight ? moveDistance : -moveDistance);
                    Canvas.SetLeft(enemy, newLeft);
                }
            }
        }


        private void AnimateEnemies(object sender, EventArgs e)
        {
            // Cycle through frame index 
            enemyFrameIndex = (enemyFrameIndex + 1) % 4; 

            foreach (var enemy in enemies)
            {
                if (enemyData.TryGetValue(enemy, out EnemyType enemyType))
                {
                    // Ensure the enemy type and its frames are valid
                    if (enemyType?.EnemyImageFrames != null && enemyType.EnemyImageFrames.Length > 0)
                    {
                        int frameCount = enemyType.EnemyImageFrames.Length;
                        int currentFrameIndex = enemyFrameIndex % frameCount; 
                        enemy.Source = LoadEnemyImage(enemyType.EnemyImageFrames, currentFrameIndex); 
                    }
                }
            }
        }

        private void EnemyShoot(object sender, EventArgs e)
        {
            if (enemies.Count == 0 || waveCleared) return;

            // Select a random enemy to shoot
            int shootingEnemyIndex = random.Next(enemies.Count);
            Image shootingEnemy = enemies[shootingEnemyIndex];

            // Get the EnemyType data for the shooter
            if (enemyData.TryGetValue(shootingEnemy, out EnemyType enemyType))
            {
                // Calculate projectile start position (center of enemy bottom)
                double enemyX = Canvas.GetLeft(shootingEnemy) + (EnemyWidth / 2.0) - 10; 
                double enemyY = Canvas.GetTop(shootingEnemy) + EnemyHeight;

                new EnemyProjectile(canvas, enemyX, enemyY, enemyType.ProjectileSpeed, enemyType.ProjectileImageFrames, gameWindow);

                // Reset the fire timer interval for the next shot (randomized and scaled)
                double fireMin = Math.Max(0.5, BaseFireIntervalMin - (currentLevel * 0.1));
                double fireMax = Math.Max(1.0, BaseFireIntervalMax - (currentLevel * 0.2));
                if (fireMin >= fireMax) fireMin = fireMax - 0.1; // Ensure min < max
                enemyFireTimer.Interval = TimeSpan.FromSeconds(random.NextDouble() * (fireMax - fireMin) + fireMin);
            }
            else
            {
                Debug.WriteLine("Error: Could not find EnemyType data for the selected shooting enemy.");
            }
        }

        // Called by Arrow when an enemy is hit 
        public void EnemyHit(Image enemy)
        {
            if (enemy != null && enemies.Contains(enemy))
            {
                if (enemyData.TryGetValue(enemy, out EnemyType type))
                    gameWindow?.AddScore(type.Points); 

                enemies.Remove(enemy);
                enemyData.Remove(enemy);
                if (canvas.Children.Contains(enemy)) 
                    canvas.Children.Remove(enemy);

                CheckWaveCleared();
            }
        }

        // Check if wave is cleared and trigger next level
        private async void CheckWaveCleared() 
        {
            if (!waveCleared && enemies.Count == 0)
            {
                waveCleared = true; 
                Debug.WriteLine($"Wave {currentLevel} cleared!");

                StopTimers(); 

                currentLevel++; 
                Debug.WriteLine($"--- Starting Level {currentLevel} ---");

                InitializeEnemies();
            }
        }
    }

    // EnemyProjectile
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
            canvas = gameCanvas;
            speed = projectileSpeed;
            frames = projectileFrames ?? new string[] { "/Resources/ball.png" }; 
            gameWindow = window; 

            // Validate frames array and load initial image safely
            if (frames.Length == 0) frames = new string[] { "/Resources/ball.png" }; 

            projectileImage = new Image
            {
                Width = 20,
                Height = 20,
                Source = LoadProjectileImage(0) 
            };

            // Check if image loaded successfully
            if (projectileImage.Source == null)
            {
                Debug.WriteLine("Error: Failed to load initial projectile image. Aborting projectile creation.");
                return; 
            }

            // Position and add to canvas
            Canvas.SetLeft(projectileImage, startX);
            Canvas.SetTop(projectileImage, startY);
            canvas.Children.Add(projectileImage);
            Canvas.SetZIndex(projectileImage, 1); 

            // Setup and start movement timer
            moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) }; 
            moveTimer.Tick += MoveProjectile;
            moveTimer.Start();

            // Setup and start animation timer only if there are multiple frames
            if (frames.Length > 1)
            {
                animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) }; 
                animationTimer.Tick += AnimateProjectile;
                animationTimer.Start();
            }
        }

        // Helper for safe projectile image loading
        private BitmapImage LoadProjectileImage(int frameIndex)
        {
            if (frames == null || frames.Length == 0) return null;
            int index = frameIndex % frames.Length; 

            try
            {
                return new BitmapImage(new Uri(frames[index], UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load projectile image frame {frames[index]}: {ex.Message}");
                return null; 
            }
        }


        private void MoveProjectile(object sender, EventArgs e)
        {
            // Ensure projectile is still valid and on the canvas
            if (projectileImage == null || !canvas.Children.Contains(projectileImage))
            {
                Cleanup();
                return;
            }

            double currentTop = Canvas.GetTop(projectileImage);
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : ((FrameworkElement)canvas.Parent)?.ActualHeight ?? 600;

            // Check if projectile has gone off the bottom of the screen
            if (currentTop >= canvasHeight)
            {
                Cleanup(); // Remove projectile
            }
            else
            {
                // Move projectile down
                Canvas.SetTop(projectileImage, currentTop + speed);
                CheckCollision();
            }
        }

        private void AnimateProjectile(object sender, EventArgs e)
        {
            // Ensure animation is needed and possible
            if (projectileImage == null || frames == null || frames.Length <= 1 || animationTimer == null || !animationTimer.IsEnabled)
            {
                animationTimer?.Stop();
                return;
            }

            frameIndex = (frameIndex + 1) % frames.Length;
            projectileImage.Source = LoadProjectileImage(frameIndex); 
            if (projectileImage.Source == null) 
            {
                Cleanup(); 
            }
        }

        private void CheckCollision()
        {
            // Ensure projectile and game context are valid before checking
            if (projectileImage == null || !canvas.Children.Contains(projectileImage) || gameWindow == null)
            {
                if (projectileImage == null) Cleanup();
                return;
            }

            // Create Rect for the projectile's current position and size
            Rect projectileRect = new Rect(
                Canvas.GetLeft(projectileImage),
                Canvas.GetTop(projectileImage),
                projectileImage.Width,
                projectileImage.Height
            );

            // Check Collision with Shields
            foreach (var shield in gameWindow.shields.ToList())
            {
                // Skip already destroyed shields
                if (shield.GetDurability() <= 0) continue;

                Image shieldImg = shield.GetImage();
                if (shieldImg == null || !canvas.Children.Contains(shieldImg)) continue;

                Rect shieldRect = new Rect(
                    Canvas.GetLeft(shieldImg),
                    Canvas.GetTop(shieldImg),
                    shieldImg.Width,
                    shieldImg.Height
                );

                // Check for intersection
                if (projectileRect.IntersectsWith(shieldRect))
                {
                    shield.TakeDamage(); 
                    Cleanup();          
                    return;              
                }
            }

            // Check Collision with Player
            Image playerImage = gameWindow.playerImage;
            if (playerImage != null && canvas.Children.Contains(playerImage))
            {
                Rect playerRect = new Rect(
                    Canvas.GetLeft(playerImage),
                    Canvas.GetTop(playerImage),
                    playerImage.Width,
                    playerImage.Height
                );

                // Check for intersection
                if (projectileRect.IntersectsWith(playerRect))
                {
                    Player player = gameWindow.player;
                    player.SetHP(player.GetHP() - 1); 
                    Debug.WriteLine($"Player hit! HP remaining: {player.GetHP()}");

                    gameWindow.UpdateHeartDisplay();

                    Cleanup();
                    if (player.GetHP() <= 0)
                    {
                        Debug.WriteLine("Player HP reached 0. Triggering Game Over.");
                        gameWindow.GameOver(false);
                    }
                    return; 
                }
            }
        }

        // Centralized method to stop timers and remove the projectile image
        private void Cleanup()
        {
            moveTimer?.Stop();
            animationTimer?.Stop();

            if (projectileImage != null && canvas.Children.Contains(projectileImage))
                canvas.Children.Remove(projectileImage);

            projectileImage = null;
            gameWindow = null; 
            canvas = null;
        }
    }
}