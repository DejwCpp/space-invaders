using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Space_intruders
{
    public class EnemyProjectile
    {
        private Image projectileImage;
        private DispatcherTimer moveTimer;
        private double speed;
        private Canvas canvas;

        public EnemyProjectile(Canvas gameCanvas, double startX, double startY, double projectileSpeed, string imagePath)
        {
            canvas = gameCanvas;
            speed = projectileSpeed;

            projectileImage = new Image
            {
                Width = 20,
                Height = 20,
                Source = new BitmapImage(new Uri(imagePath, UriKind.Relative))
            };

            Canvas.SetLeft(projectileImage, startX);
            Canvas.SetTop(projectileImage, startY);
            canvas.Children.Add(projectileImage);

            moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            moveTimer.Tick += MoveProjectile;
            moveTimer.Start();
        }

        private void MoveProjectile(object sender, EventArgs e)
        {
            double currentTop = Canvas.GetTop(projectileImage);
            if (currentTop >= ((FrameworkElement)canvas.Parent).ActualHeight)
            {
                moveTimer.Stop();
                canvas.Children.Remove(projectileImage);
            }
            else
            {
                Canvas.SetTop(projectileImage, currentTop + speed);
                CheckCollision();
            }
        }

        private void CheckCollision()
        {
            GameWindow gameWindow = (GameWindow)Application.Current.MainWindow;
            double projectileLeft = Canvas.GetLeft(projectileImage);
            double projectileTop = Canvas.GetTop(projectileImage);

            double projectileRight = projectileLeft + projectileImage.Width;
            double projectileBottom = projectileTop + projectileImage.Height;

            // Sprawdzenie kolizji z tarczami
            foreach (var shield in gameWindow.shields)
            {
                Image shieldImg = shield.GetImage();
                if (shieldImg == null) continue;



                double shieldLeft = Canvas.GetLeft(shieldImg);
                double shieldTop = Canvas.GetTop(shieldImg);
                double shieldRight = shieldLeft + shieldImg.Width;
                double shieldBottom = shieldTop + shieldImg.Height;

                bool isColliding = !(projectileRight < shieldLeft || projectileLeft > shieldRight ||
                                    projectileBottom < shieldTop || projectileTop > shieldBottom);

                if (isColliding)
                {
                    moveTimer.Stop();
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
                canvas.Children.Remove(projectileImage);
                MessageBox.Show("Uderzył Cię the rock!", "Sus", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }

    public class Enemies
    {
        private const int EnemyWidth = 50;
        private const int EnemyHeight = 50;
        private const int Columns = 10;
        private const int Rows = 4;
        private const int EnemySpacing = 10;
        private const int MoveDistance = 10;
        private const int MoveDownDistance = 15;
        private const int MoveTickDurationMs = 100;
        private bool movingRight = true;
        public List<Image> enemies = new List<Image>();
        private DispatcherTimer enemyMoveTimer;
        private DispatcherTimer enemyFireTimer;
        private DispatcherTimer enemyAnimationTimer;
        private Canvas canvas;
        private Random random = new Random();
        private string projectileImagePath = "/Resources/enemy.png";
        private double projectileSpeed = 5.0;
        private int enemyFrameIndex = 0;
        private int enemyFrameCount = 2; // Set to 2 for two images, 3 for three images
        private string[] enemyImagePaths = { "/Resources/enemy1.png", "/Resources/enemy2.png", "/Resources/enemy3.png" };
        private double loseThreshold = 490;
        public GameWindow gameWindow;


        public Enemies(Canvas gameCanvas, GameWindow gameWindow)
        {
            canvas = gameCanvas;
            this.gameWindow = gameWindow;
            StartEnemyMovement();
            StartEnemyShooting();
            StartEnemyAnimation();
        }

        public void InitializeEnemies()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    Image enemy = new Image
                    {
                        Width = EnemyWidth,
                        Height = EnemyHeight,
                        Source = new BitmapImage(new Uri(enemyImagePaths[0], UriKind.Relative))
                    };

                    Canvas.SetLeft(enemy, col * (EnemyWidth + EnemySpacing));
                    Canvas.SetTop(enemy, row * (EnemyHeight + EnemySpacing));
                    canvas.Children.Add(enemy);
                    enemies.Add(enemy);
                }
            }
            //gameWindow.enemies = enemies;

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

                // Check if the enemy has moved below the losing threshold
                if (newTop >= loseThreshold)
                {
                    enemyMoveTimer.Stop();
                    enemyFireTimer.Stop();
                    enemyAnimationTimer.Stop();

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

        private void EnemyShoot(object sender, EventArgs e)
        {
            if (enemies.Count == 0) return;

            int shootingEnemyIndex = random.Next(enemies.Count);
            Image shootingEnemy = enemies[shootingEnemyIndex];

            double enemyX = Canvas.GetLeft(shootingEnemy) + (EnemyWidth / 2) - 10;
            double enemyY = Canvas.GetTop(shootingEnemy) + EnemyHeight;

            new EnemyProjectile(canvas, enemyX, enemyY, projectileSpeed, projectileImagePath);

            enemyFireTimer.Interval = TimeSpan.FromSeconds(random.Next(1, 4));
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
            enemyFrameIndex = (enemyFrameIndex + 1) % enemyFrameCount;
            foreach (var enemy in enemies)
            {
                enemy.Source = new BitmapImage(new Uri(enemyImagePaths[enemyFrameIndex], UriKind.Relative));
            }
        }
    }
}