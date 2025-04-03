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
        // Defines the available boost types
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

        // Maps BoostType to its corresponding image resource path
        private static readonly Dictionary<BoostType, string> BoostImagePaths = new Dictionary<BoostType, string>
        {
            { BoostType.FasterShooting, "/Resources/boost_fastshoot.png" }, // icon to do
            { BoostType.HealthRegen,    "/Resources/boost_health.png" }, // icon to do
            { BoostType.TemporaryArmor, "/Resources/boost_armor.png" }, // icon to do
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
                imagePath = BoostImagePaths[BoostType.FasterShooting]; // Fallback
            }

            try
            {
                BoostImage = new Image
                {
                    Width = BoostSize,
                    Height = BoostSize,
                    Source = new BitmapImage(new Uri(imagePath, UriKind.Relative))
                };

                // Add canvas null check before adding child
                if (canvas == null)
                {
                    Debug.WriteLine($"Error: Cannot add boost image {Type}, canvas is null.");
                    BoostImage = null; // Prevent further use
                    Cleanup(); // Attempt cleanup
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
                // This catch block is crucial for image loading errors
                Debug.WriteLine($"Error creating boost image for {Type}: {ex.Message}");
                // Indicate failure by setting BoostImage to null if not already caught
                BoostImage = null;
                Cleanup(); // Ensure partial cleanup if creation fails
            }
        }

        // Moves the boost downwards and checks for collisions
        private void MoveBoost(object sender, EventArgs e)
        {
            if (BoostImage == null || canvas == null || !canvas.Children.Contains(BoostImage))
            {
                Cleanup(); return;
            }

            double currentTop = Canvas.GetTop(BoostImage);
            double newTop = currentTop + MoveSpeed;

            // Added canvas null check
            if (canvas == null || newTop >= canvas.ActualHeight)
            {
                Cleanup(); // Off screen or canvas gone
            }
            else
            {
                Canvas.SetTop(BoostImage, newTop);
                CheckCollision();
            }
        }

        // Checks for collision between the boost and the player
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
                gameWindow.ActivateBoost(this.Type); // Apply effect
                Cleanup(); // Remove boost visual
            }
        }

        // Stops timers and removes the boost image from the canvas
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
            gameWindow = null; // Break references
        }
    }
}