using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        private const int EnemyWidth = 50;
        private const int EnemyHeight = 50;
        private const int Columns = 10;
        private const int Rows = 4;
        private const int EnemySpacing = 10;
        private const int MoveDistance = 10;
        private const int MoveDownDistance = 15;
        private int enemyFrameIndex = 0;
        private const int MoveTickDurationMs = 300;
        private bool movingRight = true;
        public List<Image> enemies = new List<Image>();
        private DispatcherTimer enemyMoveTimer;
        private DispatcherTimer enemyFireTimer;
        private DispatcherTimer enemyAnimationTimer;
        private Canvas canvas;
        private Random random = new Random();
        private double loseThreshold = 490;
        public int score;
        private Dictionary<int, Image> enemiesPoints = new Dictionary<int, Image>();
        public GameWindow gameWindow;

        private EnemyType[] enemyTypes = new EnemyType[]
        {
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" },
                ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" },
                ProjectileSpeed = 7.0,
                Points = 5
            },
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/wizard/normal/animation1.png", "/Resources/wizard/normal/animation2.png", "/Resources/wizard/normal/animation3.png", "/Resources/wizard/normal/animation4.png" },
                ProjectileImageFrames = new string[] { "/Resources/wizard/bullet/bullet1.png", "/Resources/wizard/bullet/bullet2.png" },
                ProjectileSpeed = 7.0,
                Points = 10
            },
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/enemy1.png", "/Resources/enemy2.png", "/Resources/enemy1.png", "/Resources/enemy2.png" },
                ProjectileImageFrames = new string[] { "/Resources/enemy.png" },
                ProjectileSpeed = 5.0,
                Points = 15
            },
            new EnemyType {
                EnemyImageFrames = new string[] { "/Resources/enemy2.png", "/Resources/enemy1.png", "/Resources/enemy1.png", "/Resources/enemy2.png"  },
                ProjectileImageFrames = new string[] { "/Resources/ball.png" },
                ProjectileSpeed = 4.0,
                Points = 20
            },
        };

        private void CountPoints(int Points, GameWindow gameWindow)
        {
            score += Points;
            gameWindow.ScoreLabel.Content = score;
        }


        public Enemies(Canvas gameCanvas, GameWindow gameWindow)
        {
            canvas = gameCanvas;
            this.gameWindow = gameWindow;
            StartEnemyMovement();
            StartEnemyShooting();
            StartEnemyAnimation();
            CountPoints(0, gameWindow);
        }

        public void InitializeEnemies()
        {
            for (int row = 0; row < Rows; row++)
            {
                EnemyType enemyType = enemyTypes[row % enemyTypes.Length];

                for (int col = 0; col < Columns; col++)
                {
                    Image enemy = new Image
                    {
                        Width = EnemyWidth,
                        Height = EnemyHeight,
                        Source = new BitmapImage(new Uri(enemyType.EnemyImageFrames[0], UriKind.Relative)),
                        Tag = enemyType
                    };
                    Canvas.SetLeft(enemy, col * (EnemyWidth + EnemySpacing));
                    Canvas.SetTop(enemy, row * (EnemyHeight + EnemySpacing));
                    canvas.Children.Add(enemy);
                    enemies.Add(enemy);
                    //enemiesPoints[enemies] = enemyType.Points;
                }
            }
        }

        private void StartEnemyMovement()
        {
            enemyMoveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(MoveTickDurationMs)
            };
            enemyMoveTimer.Tick += MoveEnemies;
            enemyMoveTimer.Start();
        }

        private void MoveEnemies(object sender, EventArgs e)
        {
            double leftMost = double.MaxValue;
            double rightMost = double.MinValue;

            foreach (var enemy in enemies)
            {
                double currentLeft = Canvas.GetLeft(enemy);
                leftMost = Math.Min(leftMost, currentLeft);
                rightMost = Math.Max(rightMost, currentLeft);
            }

            if ((movingRight && rightMost + MoveDistance + EnemyWidth >= canvas.ActualWidth) ||
                (!movingRight && leftMost - MoveDistance <= 0))
            {
                movingRight = !movingRight;
                MoveEnemiesDown();
            }
            else
            {
                foreach (var enemy in enemies)
                {
                    double newLeft = Canvas.GetLeft(enemy) + (movingRight ? MoveDistance : -MoveDistance);
                    Canvas.SetLeft(enemy, newLeft);
                }
            }
        }

        private void MoveEnemiesDown()
        {
            foreach (var enemy in enemies)
            {
                double newTop = Canvas.GetTop(enemy) + MoveDownDistance;
                Canvas.SetTop(enemy, newTop);

                if (newTop >= loseThreshold)
                {
                    enemyMoveTimer.Stop();
                    enemyFireTimer.Stop();
                    MessageBox.Show("You lost!", "Game Over", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        private void StartEnemyShooting()
        {
            enemyFireTimer = new DispatcherTimer();
            enemyFireTimer.Interval = TimeSpan.FromSeconds(random.Next(1, 4));
            enemyFireTimer.Tick += EnemyShoot;
            enemyFireTimer.Start();
        }

        private void StartEnemyAnimation()
        {
            enemyAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(MoveTickDurationMs)
            };
            enemyAnimationTimer.Tick += AnimateEnemies;
            enemyAnimationTimer.Start();
        }

        private void AnimateEnemies(object sender, EventArgs e)
        {
            enemyFrameIndex = (enemyFrameIndex + 1) % 4; // Toggle between two frames

            foreach (var enemy in enemies)
            {
                EnemyType enemyType = (EnemyType)enemy.Tag;
                enemy.Source = new BitmapImage(new Uri(enemyType.EnemyImageFrames[enemyFrameIndex], UriKind.Relative));
            }
        }

        private void EnemyShoot(object sender, EventArgs e)
        {
            if (enemies.Count == 0) return;

            int shootingEnemyIndex = random.Next(enemies.Count);
            Image shootingEnemy = enemies[shootingEnemyIndex];
            EnemyType enemyType = (EnemyType)shootingEnemy.Tag;

            double enemyX = Canvas.GetLeft(shootingEnemy) + (EnemyWidth / 2) - 10;
            double enemyY = Canvas.GetTop(shootingEnemy) + EnemyHeight;

            new EnemyProjectile(canvas, enemyX, enemyY, enemyType.ProjectileSpeed, enemyType.ProjectileImageFrames);

            enemyFireTimer.Interval = TimeSpan.FromSeconds(random.Next(1, 4));
        }
    }

    public class EnemyProjectile
    {
        private Image projectileImage;
        private DispatcherTimer moveTimer;
        private DispatcherTimer animationTimer;
        private double speed;
        private Canvas canvas;
        private string[] frames;
        private int frameIndex = 0;

        public EnemyProjectile(Canvas gameCanvas, double startX, double startY, double projectileSpeed, string[] projectileFrames)
        {
            canvas = gameCanvas;
            speed = projectileSpeed;
            frames = projectileFrames;

            projectileImage = new Image
            {
                Width = 20,
                Height = 20,
                Source = new BitmapImage(new Uri(frames[0], UriKind.Relative))
            };

            Canvas.SetLeft(projectileImage, startX);
            Canvas.SetTop(projectileImage, startY);
            canvas.Children.Add(projectileImage);


            moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; // projectile speed but you have ProjectileSpeed varriable in EnemyType class
            moveTimer.Tick += MoveProjectile;
            moveTimer.Start();

            animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) }; // animation speed
            animationTimer.Tick += AnimateProjectile;
            animationTimer.Start();
        }

        private void MoveProjectile(object sender, EventArgs e)
        {
            double currentTop = Canvas.GetTop(projectileImage);
            if (currentTop >= ((FrameworkElement)canvas.Parent).ActualHeight)
            {
                moveTimer.Stop();
                animationTimer.Stop();
                canvas.Children.Remove(projectileImage);
            }
            else
            {
                Canvas.SetTop(projectileImage, currentTop + speed);
                CheckCollision();
            }
        }

        private void AnimateProjectile(object sender, EventArgs e)
        {
            frameIndex = (frameIndex + 1) % frames.Length;
            projectileImage.Source = new BitmapImage(new Uri(frames[frameIndex], UriKind.Relative));
        }

        private void UpdateHearts()
        {
            GameWindow gameWindow = (GameWindow)Application.Current.MainWindow;
            Player player = gameWindow.player;

            gameWindow.heartsPanel.Children.Clear();

            for (int i = 0; i < player.GetHP(); i++)
            {
                Image heartImage = new Image
                {
                    Source = new BitmapImage(new Uri("/Resources/heart.png", UriKind.Relative)),
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                gameWindow.heartsPanel.Children.Add(heartImage);
            }
        }

        private void CheckCollision()
        {
            GameWindow gameWindow = (GameWindow)Application.Current.MainWindow;
            Player player = gameWindow.player;

            double projectileLeft = Canvas.GetLeft(projectileImage);
            double projectileTop = Canvas.GetTop(projectileImage);
            double projectileRight = projectileLeft + projectileImage.Width;
            double projectileBottom = projectileTop + projectileImage.Height;

            // Sprawdzenie kolizji z tarczami
            foreach (var shield in gameWindow.shields)
            {
                Image shieldImg = shield.GetImage();

                if (shieldImg.Tag.ToString() == "destroyed")
                    continue;

                double shieldLeft = Canvas.GetLeft(shieldImg);
                double shieldTop = Canvas.GetTop(shieldImg);
                double shieldRight = shieldLeft + shieldImg.Width;
                double shieldBottom = shieldTop + shieldImg.Height;

                bool isColliding = !(projectileRight < shieldLeft || projectileLeft > shieldRight ||
                                    projectileBottom < shieldTop || projectileTop > shieldBottom);

                if (isColliding)
                {
                    moveTimer.Stop();
                    animationTimer.Stop();
                    canvas.Children.Remove(projectileImage);
                    shield.TakeDamage();
                    return;
                }
            }

            Image playerImage = gameWindow.playerImage;
            double playerLeft = Canvas.GetLeft(playerImage);
            double playerTop = Canvas.GetTop(playerImage);
            double playerRight = playerLeft + playerImage.Width;
            double playerBottom = playerTop + playerImage.Height;

            bool playerColliding = !(projectileRight < playerLeft || projectileLeft > playerRight ||
                                   projectileBottom < playerTop || projectileTop > playerBottom);

            if (playerColliding)
            {
                moveTimer.Stop();
                animationTimer.Stop();
                canvas.Children.Remove(projectileImage);

                player.SetHP(player.GetHP() - 1);
                UpdateHearts();

                if (player.GetHP() <= 0)
                {
                    MessageBox.Show("Dedło Ci się!", "Wasted", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                }
            }
        }

    }
}