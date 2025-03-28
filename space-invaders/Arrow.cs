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
            arrowMoveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(25)
            };
            arrowMoveTimer.Tick += (s, e) => MoveAndCheck();
            arrowMoveTimer.Start();
        }

        private void MoveAndCheck()
        {
            if (ArrowImage == null || gameWindow == null || !gameWindow.gameCanvas.Children.Contains(ArrowImage))
            {
                arrowMoveTimer?.Stop();
                if (gameWindow != null && gameWindow.arrows.ContainsKey(ID))
                {
                    gameWindow.Dispatcher.Invoke(() => gameWindow.RemoveArrow(ID));
                }
                return;
            }

            gameWindow.Dispatcher.Invoke(() =>
            {
                if (!gameWindow.gameCanvas.Children.Contains(ArrowImage))
                {
                    arrowMoveTimer?.Stop();
                    gameWindow.RemoveArrow(ID);
                    return;
                }

                double currentTop = Canvas.GetTop(ArrowImage);
                double newTop = currentTop - Speed;
                Canvas.SetTop(ArrowImage, newTop);

                bool collisionOccurred = CheckCollisions();

                if (!collisionOccurred && newTop < -ArrowImage.Height)
                {
                    arrowMoveTimer.Stop();
                    gameWindow.RemoveArrow(ID);
                }
            });
        }

        private bool CheckCollisions()
        {
            if (ArrowImage == null || gameWindow == null) return false;

            Rect arrowRect = new Rect(
                Canvas.GetLeft(ArrowImage),
                Canvas.GetTop(ArrowImage),
                ArrowImage.Width,
                ArrowImage.Height
            );

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
                    arrowMoveTimer.Stop();
                    gameWindow.RemoveArrow(ID);
                    return true;
                }
            }

            Image lowestHitEnemy = null;
            double maxEnemyTop = double.MinValue;

            List<Image> currentEnemies = gameWindow.enemies.enemies.ToList();

            foreach (var enemy in currentEnemies)
            {
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
                if (gameWindow.enemies.enemies.Contains(lowestHitEnemy))
                {
                    gameWindow.enemies.enemies.Remove(lowestHitEnemy);
                    gameWindow.gameCanvas.Children.Remove(lowestHitEnemy);
                }

                arrowMoveTimer.Stop();
                gameWindow.RemoveArrow(ID);
                return true;
            }

            return false;
        }
    }
}