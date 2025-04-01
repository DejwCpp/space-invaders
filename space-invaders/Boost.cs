using Space_intruders;

public class Boost
{
    private Image boostImage;
    private DispatcherTimer moveTimer;
    private Canvas canvas;
    private GameWindow gameWindow;
    private string type;

    public Boost(Canvas gameCanvas, double startX, double startY, GameWindow window)
    {
        canvas = gameCanvas;
        gameWindow = window;

        string[] boostTypes = { "arrow_speed", "extra_life", "shield", "super_bullet" };
        type = boostTypes[new Random().Next(boostTypes.Length)];

        boostImage = new Image
        {
            Width = 35,
            Height = 35,
        //    Source = new BitmapImage(new Uri($"/Resources/{type}.png", UriKind.Relative))
        };

        Canvas.SetLeft(boostImage, startX);
        Canvas.SetTop(boostImage, startY);
        canvas.Children.Add(boostImage);

        moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        moveTimer.Tick += MoveBoost;
        moveTimer.Start();
    }

    private void MoveBoost(object sender, EventArgs e)
    {
        double currentTop = Canvas.GetTop(boostImage);
        if (currentTop >= canvas.ActualHeight)
        {
            moveTimer.Stop();
            canvas.Children.Remove(boostImage);
        }
        else
        {
            Canvas.SetTop(boostImage, currentTop + 5);
            CheckCollision();
        }
    }

    private void CheckCollision()
    {
        Image playerImage = gameWindow.playerImage;
        double playerLeft = Canvas.GetLeft(playerImage);
        double playerTop = Canvas.GetTop(playerImage);
        double playerRight = playerLeft + playerImage.Width;
        double playerBottom = playerTop + playerImage.Height;

        double boostLeft = Canvas.GetLeft(boostImage);
        double boostTop = Canvas.GetTop(boostImage);
        double boostRight = boostLeft + boostImage.Width;
        double boostBottom = boostTop + boostImage.Height;

        bool isColliding = !(boostRight < playerLeft || boostLeft > playerRight ||
                            boostBottom < playerTop || boostTop > playerBottom);

        if (isColliding)
        {
            moveTimer.Stop();
            canvas.Children.Remove(boostImage);
            ApplyBoost();
        }
    }
    /*
    private void ApplyBoost()
    {
        switch (type)
        {
            case "arrow_speed":
                gameWindow.ReduceShootCooldown();
                break;
            case "extra_life":
                gameWindow.AddLife();
                break;
            case "shield":
                gameWindow.ActivateSuperBullet();
                break;
            case "super_bullet":
                gameWindow.ActivateShield();
                break;
        }
    }
    */
}