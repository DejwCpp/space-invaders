using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;

namespace Space_intruders
{
    public class Arrow
    {
        private int ID;
        private int Speed;
        private int DMG;
        private Image ArrowImage;
        private GameWindow gameWindow;
        private DispatcherTimer arrowMoveTimer;
        private bool isMoving = false; 


        // Getters/Setters
        public int GetID() { return this.ID; }
        public int GetSpeed() { return this.Speed; }
        public int GetDMG() { return this.DMG; }
        public Image GetImageSource() { return this.ArrowImage; }
        public void SetID(int id) { this.ID = id; }
        public void SetSpeed(int speed) { this.Speed = speed; }
        public void SetDMG(int dmg) { this.DMG = dmg; }
        public void SetImageSource(Image source) { this.ArrowImage = source; }
        public void SetGameWindow(GameWindow window) { this.gameWindow = window; }

 
        public void SetTimer()
        {
            // Prevent starting multiple timers for the same arrow
            if (isMoving) return;

            arrowMoveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20) 
            };
            arrowMoveTimer.Tick += MoveAndCheck;
            arrowMoveTimer.Start();
            isMoving = true;
        }

        public void StopTimer()
        {
            arrowMoveTimer?.Stop();
            isMoving = false;
        }


        private void MoveAndCheck(object sender, EventArgs e) 
        {
            gameWindow?.Dispatcher.Invoke(() =>
            {
                // Basic validity checks
                if (ArrowImage == null || gameWindow == null || !gameWindow.gameCanvas.Children.Contains(ArrowImage) || !isMoving)
                {
                    StopTimer();           
                    gameWindow?.RemoveArrow(ID);
                    return;
                }

                double currentTop = Canvas.GetTop(ArrowImage);
                double newTop = currentTop - Speed;
                Canvas.SetTop(ArrowImage, newTop);

                bool collisionOccurred = CheckCollisions();

                // Remove arrow if it goes off-screen AND no collision happened
                if (!collisionOccurred && newTop < -ArrowImage.Height)
                {
                    StopTimer();
                    gameWindow.RemoveArrow(ID);
                }
            });
        }
        
        private bool CheckCollisions()
        {
            if (ArrowImage == null || gameWindow == null || !isMoving) return false;

            Rect arrowRect = new Rect(
                Canvas.GetLeft(ArrowImage),
                Canvas.GetTop(ArrowImage),
                ArrowImage.Width,
                ArrowImage.Height
            );


            // Check Shield Collisions
            foreach (var shield in gameWindow.shields.ToList())
            {
                if (shield.GetDurability() <= 0) continue;

                Image shieldImage = shield.GetImage();
                if (shieldImage == null || !gameWindow.gameCanvas.Children.Contains(shieldImage)) continue;


                Rect shieldRect = new Rect(
                    Canvas.GetLeft(shieldImage),
                    Canvas.GetTop(shieldImage),
                    shieldImage.Width,
                    shieldImage.Height
                );

                if (arrowRect.IntersectsWith(shieldRect))
                {
                    shield.TakeDamage();
                    StopTimer(); 
                    gameWindow.RemoveArrow(ID);
                    return true;
                }
            }

            // Check Enemy Collisions
            Image lowestHitEnemy = null;
            double maxEnemyTop = double.MinValue; 

            // Iterate over a copy of the enemy list for safety
            List<Image> currentEnemies = gameWindow.enemies?.enemies?.ToList() ?? new List<Image>();


            foreach (var enemy in currentEnemies)
            {
                // Ensure enemy is valid and still on the canvas
                if (enemy == null || !gameWindow.gameCanvas.Children.Contains(enemy)) continue;


                Rect enemyRect = new Rect(
                    Canvas.GetLeft(enemy),
                    Canvas.GetTop(enemy),
                    enemy.Width,
                    enemy.Height
                );

                if (arrowRect.IntersectsWith(enemyRect))
                {
                    double currentEnemyTop = Canvas.GetTop(enemy);
         
                    if (currentEnemyTop > maxEnemyTop)
                    {
                        maxEnemyTop = currentEnemyTop;
                        lowestHitEnemy = enemy;
                    }
                }
            }

            
            if (lowestHitEnemy != null)
            {
                gameWindow.enemies?.EnemyHit(lowestHitEnemy); 

                StopTimer(); 
                gameWindow.RemoveArrow(ID); 
                return true; 
            }

            return false; 
        }
    }
}