using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Diagnostics; 
public class Shield
{
    private Canvas canvas;
    private Image shieldImage;
    private int initialDurability = 7;
    private int durability;
    private double initialPosX;
    private double initialPosY;

    private static readonly string[] shieldImages =
    {
        "/Resources/shield/castle9.png",
        "/Resources/shield/castle8.png",
        "/Resources/shield/castle7.png",
        "/Resources/shield/castle6.png",
        "/Resources/shield/castle5.png",
        "/Resources/shield/castle4.png",
        "/Resources/shield/castle3.png",
        "/Resources/shield/castle2.png",
        "/Resources/shield/castle1.png",
    };

    public Shield(Canvas gameCanvas, double posX, double posY)
    {
        canvas = gameCanvas;
        initialPosX = posX; 
        initialPosY = posY;
        durability = initialDurability;

        shieldImage = new Image()
        {
            Width = 110,
            Height = 100,
            Source = LoadShieldImage(8), 
            Tag = "active" 
        };

        Canvas.SetLeft(shieldImage, posX - shieldImage.Width / 2);
        Canvas.SetTop(shieldImage, posY);
        canvas.Children.Add(shieldImage);
    }

    // Helper to load images safely
    private BitmapImage LoadShieldImage(int index)
    {
        if (index < 0 || index >= shieldImages.Length) index = 0; 
        try
        {
            return new BitmapImage(new Uri(shieldImages[index], UriKind.Relative));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading shield image {shieldImages[index]}: {ex.Message}");
            return new BitmapImage(new Uri(shieldImages[0], UriKind.Relative));
        }
    }

    public Image GetImage() => shieldImage;
    public int GetDurability() => durability;

    public void TakeDamage()
    {
        if (durability <= 0)
        {
            Debug.WriteLine("Shield destroyed.");
            if (canvas.Children.Contains(shieldImage))
            {
                canvas.Children.Remove(shieldImage);
            }
            shieldImage.Tag = "destroyed";

            return;
        }

        durability--;

        shieldImage.Source = LoadShieldImage(durability);
    }
}