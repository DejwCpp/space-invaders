using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Numerics;

namespace Space_intruders
{
    public partial class GameWindow : Window
    {
        int marginPoz = 370;
        Player player = new();
        static int counter = 0;
        public Dictionary<int, Arrow> arrows = new Dictionary<int, Arrow>();
        public List<Shield> shields = new List<Shield>();
        public Enemies enemies { get; private set; }
        bool canShoot = true;

        public GameWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(PlayerMovement);
            InitializePlayer();
            enemies = new Enemies(gameCanvas, this);
            enemies.InitializeEnemies();
            InitializeShields();
        }

        private void InitializePlayer()
        {
            player.SetID(0);
            player.SetHP(3);
            player.SetSpeed(20);
            player.SetArmour(1);
            player.SetDMG(1);
        }
        private void InitializeShields()
        {
            double[] shieldPositions = { 50, 200, 350 };

            foreach (double posX in shieldPositions)
            {
                shields.Add(new Shield(gameCanvas, posX, 400));
            }
        }
        public void SpawnNewArrow()
        {
            try
            {
                Arrow arrow = new Arrow();
                Interlocked.Increment(ref counter);
                arrow.SetID(counter);
                arrow.SetDMG(player.GetDMG());
                arrow.SetSpeed(10);
                arrow.SetGameWindow(this);

                // Create the arrow image
                BitmapImage imageSource = new BitmapImage(new Uri("/Resources/arrow.png", UriKind.Relative));

                Image arrowImage = new Image()
                {
                    Source = imageSource,
                    Width = 30,
                    Height = 30,
                };

                // Position the arrow at the player's position
                double playerTop = Canvas.GetTop(playerImage);
                Canvas.SetLeft(arrowImage, Canvas.GetLeft(playerImage) + playerImage.Width / 2 - arrowImage.Width / 2);
                Canvas.SetTop(arrowImage, playerTop - arrowImage.Height);

                Debug.WriteLine($"Arrow created at position: Left={Canvas.GetLeft(arrowImage)}, Top={Canvas.GetTop(arrowImage)}");

                gameCanvas.Children.Add(arrowImage);
                arrow.SetImageSource(arrowImage);

                arrows.Add(arrow.GetID(), arrow);

                arrow.SetTimer();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating arrow: " + ex.Message);
            }
        }

        public void RemoveArrow(int arrowID)
        {
            if (arrows.TryGetValue(arrowID, out Arrow arrow))
            {
                Dispatcher.Invoke(() => {
                    gameCanvas.Children.Remove(arrow.GetImageSource());
                    arrows.Remove(arrowID);
                });
            }
        }

        public async void PlayerMovement(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                case Key.D:
                    marginPoz += player.GetSpeed();
                    Canvas.SetLeft(playerImage, marginPoz);
                    break;
                case Key.Left:
                case Key.A:
                    marginPoz -= player.GetSpeed();
                    Canvas.SetLeft(playerImage, marginPoz);
                    break;
                case Key.Space:
                    if (canShoot)
                    {
                        canShoot = false;
                        SpawnNewArrow();
                        await Task.Delay(500);
                        canShoot = true;
                    }
                    break;
            }
        }
    }
}
