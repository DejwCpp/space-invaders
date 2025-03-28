using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
public class Shield
{
    private Canvas canvas;
    private Image shieldImage;
    private int durability;

    private string[] shieldImages =
    {
        "/Resources/shield3.png",
        "/Resources/shield2.png",
        "/Resources/shield.png"
    };

    public Shield(Canvas gameCanvas, double posX, double posY)
    {
        canvas = gameCanvas;
        durability = 3;

        shieldImage = new Image()
        {
            Width = 70,
            Height = 53,
            Source = new BitmapImage(new Uri(shieldImages[0], UriKind.Relative))
        };

        Canvas.SetLeft(shieldImage, posX - shieldImage.Width / 2);
        Canvas.SetTop(shieldImage, posY);
        canvas.Children.Add(shieldImage);
    }

    public Image GetImage() => shieldImage;
    public int GetDurability() => durability;

    public void TakeDamage()
    {
        durability--;

        if (durability > 0)
        {
            shieldImage.Source = new BitmapImage(new Uri(shieldImages[3 - durability], UriKind.Relative));
        }
        else
        {
            canvas.Children.Remove(shieldImage);
        }
    }
}