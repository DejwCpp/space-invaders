using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Space_intruders
{
    public class Boost
    {
        public enum BoostType
        {
            FasterShooting,
            HealthRegen,
            TemporaryArmor,
            SlowEnemies
        }

        public Image BoostImage { get; private set; }
        public BoostType Type { get; private set; }

        private DispatcherTimer moveTimer;
        private Canvas canvas;
        private GameWindow gameWindow;
        private static readonly double MoveSpeed = 3.0;
        private static readonly int BoostSize = 30;

        private static readonly Dictionary<BoostType, string> BoostImagePaths = new Dictionary<BoostType, string>
        {
            { BoostType.FasterShooting, "/Resources/boost_fastshoot.png" },
            { BoostType.HealthRegen,    "/Resources/boost_health.png" },
            { BoostType.TemporaryArmor, "/Resources/boost_armor.png" },
            { BoostType.SlowEnemies,    "/Resources/boost_slow.png" }
        };

        public Boost(Canvas gameCanvas, double startX, double startY, BoostType boostType, GameWindow window)
        {
            canvas = gameCanvas;
            gameWindow = window;
            Type = boostType;

            if (!BoostImagePaths.TryGetValue(Type, out string imagePath))
            {
                Debug.WriteLine($"Warning: No image path defined for BoostType.{Type}. Using default.");
                imagePath = BoostImagePaths[BoostType.FasterShooting];
            }

            try
            {
                BoostImage = new Image
                {
                    Width = BoostSize,
                    Height = BoostSize,
                    Source = new BitmapImage(new Uri(imagePath, UriKind.Relative))
                };

                if (canvas == null)
                {
                    Debug.WriteLine($"Error: Cannot add boost image {Type}, canvas is null.");
                    BoostImage = null;
                    Cleanup();
                    return;
                }

                Canvas.SetLeft(BoostImage, startX - BoostSize / 2.0);
                Canvas.SetTop(BoostImage, startY);
                Canvas.SetZIndex(BoostImage, 5);
                canvas.Children.Add(BoostImage);

                moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                moveTimer.Tick += MoveBoost;
                moveTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating boost image for {Type}: {ex.Message}");
                BoostImage = null;
                Cleanup();
            }
        }

        private void MoveBoost(object sender, EventArgs e)
        {
            if (BoostImage == null || canvas == null || !canvas.Children.Contains(BoostImage))
            {
                Cleanup(); return;
            }

            double currentTop = Canvas.GetTop(BoostImage);
            double newTop = currentTop + MoveSpeed;

            if (canvas == null || newTop >= 580) // Use design height
            {
                Cleanup();
            }
            else
            {
                Canvas.SetTop(BoostImage, newTop);
                CheckCollision();
            }
        }

        private void CheckCollision()
        {
            if (BoostImage == null || gameWindow?.playerImage == null || canvas == null || !canvas.Children.Contains(gameWindow.playerImage))
            {
                return;
            }

            Rect boostRect = new Rect(Canvas.GetLeft(BoostImage), Canvas.GetTop(BoostImage), BoostImage.Width, BoostImage.Height);
            Rect playerRect = new Rect(Canvas.GetLeft(gameWindow.playerImage), Canvas.GetTop(gameWindow.playerImage), gameWindow.playerImage.Width, gameWindow.playerImage.Height);

            if (boostRect.IntersectsWith(playerRect))
            {
                gameWindow.ActivateBoost(this.Type);
                Cleanup();
            }
        }

        public void Cleanup()
        {
            moveTimer?.Stop();
            moveTimer = null;

            if (BoostImage != null && canvas != null && canvas.Children.Contains(BoostImage))
            {
                canvas.Children.Remove(BoostImage);
            }
            BoostImage = null;
            canvas = null;
            gameWindow = null;
        }
    }
}