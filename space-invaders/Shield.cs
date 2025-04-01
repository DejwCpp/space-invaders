using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Diagnostics; 
public class Shield
{
    private Canvas canvas;
    private Image shieldImage;
    private int initialDurability = 6;
    private int durability;
    private double initialPosX;
    private double initialPosY;

    private static readonly string[] shieldImages = 
    {
        "/Resources/shield3.png", 
        "/Resources/shield2.png", 
        "/Resources/shield.png"   
    };

    public Shield(Canvas gameCanvas, double posX, double posY)
    {
        canvas = gameCanvas;
        initialPosX = posX; 
        initialPosY = posY;
        durability = initialDurability;

        shieldImage = new Image()
        {
            Width = 70,
            Height = 53,
            Source = LoadShieldImage(0), 
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
        if (durability <= 0) return; 

        durability--;

        if (durability == 3) 
        {
            shieldImage.Source = LoadShieldImage(1);
        }
        else if (durability == 1) 
        {
            shieldImage.Source = LoadShieldImage(2);
        }
        else if (durability <= 0) 
        {
            Debug.WriteLine("Shield destroyed.");
            if (canvas.Children.Contains(shieldImage))
            {
                canvas.Children.Remove(shieldImage);
            }
            shieldImage.Tag = "destroyed";
        }
    }
}