/// PS8 implementation done by Josie Fiedel & Braden Fiedel
/// November 28, 2022
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using System.Reflection;

namespace SnakeGame;

/// <summary>
/// Class for drawing the elements in the view (background, snakes, powerups, walls).
/// </summary>
public class WorldPanel : IDrawable
{
    private readonly GraphicsView graphicsView;

    private IImage texture;
    private IImage powerup1, powerup2;
    private IImage snakeFaceUp, snakeFaceDown, snakeFaceLeft, snakeFaceRight;
    private IImage exp0, exp1, exp2, exp3, exp4, exp5, exp6, exp7, exp8,
        exp9, exp10, exp11, exp12, exp13, exp14, exp15, exp16;

    private World theWorld;                 // Container for the game objects. 
    private readonly int viewSize = 900;    // Default viewSize.
    private int myID;                       // Unique client ID. 

    private bool initializedForDrawing = false;     // Whether or not images are loaded.

#if MACCATALYST

    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    private IImage loadImage( string name )
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream( assembly.GetManifestResourceStream( $"{path}.{name}" ) );
    }
#endif

    public WorldPanel()
    {
        graphicsView = new();
        graphicsView.Drawable = this;
    }

    /// <summary>
    /// Sets the world and the client's ID. 
    /// </summary>
    /// <param name="world"> Server world </param>
    /// <param name="ID"> Client's ID </param>
    public void SetWorldAndID(World world, int ID)
    {
        theWorld = world;
        myID = ID;
        graphicsView.WidthRequest = theWorld.size;
        graphicsView.HeightRequest = theWorld.size;
    }

    /// <summary>
    /// Loads all of the images necessary to draw the world. 
    /// </summary>
    private void InitializeDrawing()
    {
        // Image for the background.
        texture = loadImage("Texture.png");

        // Images for powerups.
        powerup1 = loadImage("Powerup1.png");
        powerup2 = loadImage("Powerup2.png");

        // Images for the snake's face. 
        snakeFaceUp = loadImage("SnakeFaceUp.png");
        snakeFaceDown = loadImage("SnakeFaceDown.png");
        snakeFaceLeft = loadImage("SnakeFaceLeft.png");
        snakeFaceRight = loadImage("SnakeFaceRight.png");

        // Images for the snake explosion death animation.
        exp0 = loadImage("exp0.png");
        exp1 = loadImage("exp1.png");
        exp2 = loadImage("exp2.png");
        exp3 = loadImage("exp3.png");
        exp4 = loadImage("exp4.png");
        exp5 = loadImage("exp5.png");
        exp6 = loadImage("exp6.png");
        exp7 = loadImage("exp7.png");
        exp8 = loadImage("exp8.png");
        exp9 = loadImage("exp9.png");
        exp10 = loadImage("exp10.png");
        exp11 = loadImage("exp11.png");
        exp12 = loadImage("exp12.png");
        exp13 = loadImage("exp13.png");
        exp14 = loadImage("exp14.png");
        exp15 = loadImage("exp15.png");
        exp16 = loadImage("exp16.png");

        initializedForDrawing = true;
    }

    /// <summary>
    /// Color picker for each snake object, chosen depending on the snake ID. 
    /// </summary>
    /// <param name="id"> Snake ID </param>
    /// <returns> The snake's hex color </returns>
    private static Color ChooseColor(long id)
    {
        switch (id % 8)
        { 
            case 0:
                return Color.FromRgba("FF2F8E");
            case 1:
                return Color.FromRgba("FF9E4C");
            case 2:
                return Color.FromRgba("FFD600");
            case 3:
                return Color.FromRgba("66DF48");
            case 4:
                return Color.FromRgba("6A77DD");
            case 5:
                return Color.FromRgba("9803CE");
            case 6:
                return Color.FromRgba("ED0003");
            default:
                return Color.FromRgba("FFFFFF");
        }
    }

    /// <summary>
    /// Draws the current state of the world, including the translated view, the background, snakes,
    /// walls, and powerup objects. The background is composed of a patterned backdrop with a translucent
    /// overlay that changes color depending on the client's position in the world. 
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if(theWorld != null && theWorld.snakes.ContainsKey(myID))
        {
            if (!initializedForDrawing)
                InitializeDrawing();

            // Undo any leftover canvas transformations from the last frame. 
            canvas.ResetState();

            // Center the player's view on the coordinates of the snake's head.
            GetSnakeHead(out float headX, out float headY);
            canvas.Translate(-headX + (viewSize / 2), -headY + (viewSize / 2));

            // Draw the background.
            canvas.DrawImage(texture, -theWorld.size / 2, -theWorld.size / 2, theWorld.size, theWorld.size);

            float backgroundHue = (float)(Math.Sqrt(headX * headX + headY * headY) / (theWorld.size / 2));
            canvas.FillColor = Color.FromHsva(backgroundHue, 1, 1, .25f);
            canvas.FillRectangle(-theWorld.size / 2, -theWorld.size / 2, theWorld.size, theWorld.size);

            // Draw the world Wall objects.
            lock (theWorld.walls)
            {
                foreach (Wall w in theWorld.walls.Values)
                    WallDrawer(canvas, w); 
            }

            // Draw the world Powerup objects.
            lock (theWorld.powerups)
            {
                foreach (Powerup p in theWorld.powerups.Values)
                    PowerupDrawer(canvas, p);
            }

            // Draw the world Snake objects.
            lock (theWorld.snakes)
            {
                foreach (Snake s in theWorld.snakes.Values)
                    SnakeDrawer(canvas, s);
            }
        }
    }

    /// <summary>
    /// Calculates the X and Y coordinate positions of the client's snake's head. 
    /// </summary>
    /// <param name="headX"> X-coordinate of the client's snake's head </param>
    /// <param name="headY"> Y-coordinate of the client's snake's head </param>
    private void GetSnakeHead(out float headX, out float headY)
    {
        headX = (float)theWorld.snakes[myID].body.Last().X;
        headY = (float)theWorld.snakes[myID].body.Last().Y;
    }

    /// <summary>
    /// Draws the Snake object on the canvas. The snake is drawn with its name and score
    /// displayed above its head. If the snake wraps around the world map, it is ensured that the
    /// body segment spanning across the map is not drawn. If the snake dies, an explosion
    /// animation occurs. The snake is drawn with a stroke of width 10 pixels. 
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="snake"> The Snake object to draw </param>
    private void SnakeDrawer(ICanvas canvas, Snake snake)
    {
        canvas.SetShadow(new SizeF(0, 0), 0, Colors.Black); // Reset the shadow previously made for the text.
        canvas.StrokeSize = 10;
        canvas.StrokeColor = ChooseColor(snake.ID);         // Snake color is based off of its ID.
        canvas.StrokeLineCap = LineCap.Round;

        float prevX = (float)snake.body.First().X;
        float prevY = (float)snake.body.First().Y;

        // Draws the individual segments of the snake's body.
        for (int i = 1; i < snake.body.Count; i++)
        {
            // If the snake wraps around the world, ensure that the body segments are drawn correctly.
            if ((Math.Abs(prevX) >= theWorld.size / 2 && Math.Abs(snake.body[i].X) >= theWorld.size / 2) || 
                (Math.Abs(prevY) >= theWorld.size / 2 && Math.Abs(snake.body[i].Y) >= theWorld.size / 2))
            {
                prevX = (float)snake.body[i].X;
                prevY = (float)snake.body[i].Y;
                continue;
            }
            canvas.DrawLine(prevX, prevY, (float)snake.body[i].X, (float)snake.body[i].Y);
            prevX = (float)snake.body[i].X;
            prevY = (float)snake.body[i].Y;
        }

        // Draw the snake's tongue depending on the snake's orientation.
        if (snake.dir.Y < 0)
            canvas.DrawImage(snakeFaceUp, prevX - 5, prevY - 15, 10, 22);
        else if (snake.dir.Y > 0)
            canvas.DrawImage(snakeFaceDown, prevX - 5, prevY - 7, 10, 22);
        else if (snake.dir.X < 0)
            canvas.DrawImage(snakeFaceLeft, prevX - 15, prevY - 5, 22, 10);
        else
            canvas.DrawImage(snakeFaceRight, prevX - 7, prevY - 5, 22, 10);

        // Draw the player's name and score above the snake head.
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontColor = Colors.White; 
        canvas.SetShadow(new SizeF(1, 1), 3, Colors.Black);
        canvas.DrawString(snake.name + ": " + snake.score, prevX, prevY - 20, HorizontalAlignment.Center);

        // If the snake dies, begin the explosion animation.
        if (snake.died)
            snake.animationStatus = true;

        // Continue the explosion animation when appropriate. 
        if (snake.animationStatus)
        {
            // Reset animation process once finished or respawned. 
            if (snake.animationFrame == 85 || snake.alive)
            {
                snake.animationStatus = false;
                snake.animationFrame = 0;
                return;
            }
            DrawExplosion(canvas, (float)prevX - 35, (float)prevY - 50, snake.animationFrame);
            snake.animationFrame++;
        }
    }

    /// <summary>
    /// Draws the powerup on the canvas. The style of powerup is chosen depending on the
    /// powerup's ID. The background color of the powerup is drawn dynamically. Similar
    /// to the world background, the color changes as the player's snake moves around the
    /// world. The powerup is drawn 16 x 16.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="powerup"> The powerup to draw. </param>
    private void PowerupDrawer(ICanvas canvas, Powerup powerup)
    {
        // Draw the powerup's inner dynamic color.
        GetSnakeHead(out float headX, out float headY);
        float hue = (float)(Math.Sqrt(headX * headX + headY * headY) / (theWorld.size / 2));
        canvas.FillColor = Color.FromHsva((float)(hue + .05), 1, 1, .25f);

        float xPos = (float)powerup.loc.X;
        float yPos = (float)powerup.loc.Y;
        int width = 16;
        canvas.FillCircle(xPos, yPos, width / 2);

        // Draw the appropriate powerup image.
        if (powerup.ID % 2 == 0)
            canvas.DrawImage(powerup1, xPos - (width / 2), yPos - (width / 2), width, width);
        else
            canvas.DrawImage(powerup2, xPos - (width / 2), yPos - (width / 2), width, width);
    }

    /// <summary>
    /// Draws the wall on the canvas. Vertical and horizontal walls may be sent by the server. Each are
    /// drawn differently. The background color of the wall is drawn dynamically. Similar to the world
    /// background and the powerup object, the color changes as the player's snake moves around the world.
    /// The wall is drawn 50 x 50.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="wall"> The wall to draw </param>
    private void WallDrawer(ICanvas canvas, Wall wall)
    {
        GetSnakeHead(out float headX, out float headY);
        canvas.StrokeSize = 5;
        float hue = (float)(Math.Sqrt(headX * headX + headY * headY) / (theWorld.size / 2));
        canvas.FillColor = Color.FromHsva((float)(hue + .05), 1, 1, .25f);

        int wallCount;
        int width = 50;
        // Draws a vertical wall with an inner dynamic color and a black outline.
        if (wall.p1.X == wall.p2.X)
        {
            int yDisplacement = (int)(wall.p1.Y - wall.p2.Y);
            wallCount = (Math.Abs(yDisplacement) + width) / width;
            for (int i = 0; i < wallCount; i++)
            {
                if (yDisplacement > 0)
                {
                    canvas.FillRectangle((float)(wall.p1.X - width / 2), (float)(wall.p1.Y - width / 2 - (width * i)), width, width);
                    canvas.DrawRectangle((float)(wall.p1.X - width / 2), (float)(wall.p1.Y - width / 2 - (width * i)), width, width);
                }
                else
                {
                    canvas.FillRectangle((float)(wall.p1.X - width / 2), (float)(wall.p1.Y - width / 2 + (width * i)), width, width);
                    canvas.DrawRectangle((float)(wall.p1.X - width / 2), (float)(wall.p1.Y - width / 2 + (width * i)), width, width);
                }
            }
        }
        // Draws a horizontal wall with an inner dynamic color and a black outline.
        else
        {
            int xDisplacement = (int)(wall.p1.X - wall.p2.X);
            wallCount = (Math.Abs(xDisplacement) + width) / width;
            for (int i = 0; i < wallCount; i++)
            {
                if (xDisplacement > 0)
                {
                    canvas.FillRectangle((float)(wall.p1.X - width / 2 - (width * i)), (float)(wall.p1.Y - width / 2), width, width);
                    canvas.DrawRectangle((float)(wall.p1.X - width / 2 - (width * i)), (float)(wall.p1.Y - width / 2), width, width);
                }
                else
                {
                    canvas.FillRectangle((float)(wall.p1.X - width / 2 + (width * i)), (float)(wall.p1.Y - width / 2), width, width);
                    canvas.DrawRectangle((float)(wall.p1.X - width / 2 + (width * i)), (float)(wall.p1.Y - width / 2), width, width);
                }
            }
        }
    }

    /// <summary>
    /// Draws the images for the snake explosion death animation. Each explosion image is drawn
    /// for 5 frames, or until the snake respawns. 
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="headX"> X-coordinate of the head of the snake </param>
    /// <param name="headY"> Y-coordinate of the head of the snake </param>
    /// <param name="animationFrame"> Number representing the animation frame </param>
    public void DrawExplosion(ICanvas canvas, float headX, float headY, int animationFrame)
    {
        int frame = animationFrame / 5;
        switch (frame)
        {
            case 0:
                canvas.DrawImage(exp0, headX, headY, 71, 100);
                return;
            case 1:
                canvas.DrawImage(exp1, headX, headY, 71, 100);
                return;
            case 2:
                canvas.DrawImage(exp2, headX, headY, 71, 100);
                return;
            case 3:
                canvas.DrawImage(exp3, headX, headY, 71, 100);
                return;
            case 4:
                canvas.DrawImage(exp4, headX, headY, 71, 100);
                return;
            case 5:
                canvas.DrawImage(exp5, headX, headY, 71, 100);
                return;
            case 6:
                canvas.DrawImage(exp6, headX, headY, 71, 100);
                return;
            case 7:
                canvas.DrawImage(exp7, headX, headY, 71, 100);
                return;
            case 8:
                canvas.DrawImage(exp8, headX, headY, 71, 100);
                return;
            case 9:
                canvas.DrawImage(exp9, headX, headY, 71, 100);
                return;
            case 10:
                canvas.DrawImage(exp10, headX, headY, 71, 100);
                return;
            case 11:
                canvas.DrawImage(exp11, headX, headY, 71, 100);
                return;
            case 12:
                canvas.DrawImage(exp12, headX, headY, 71, 100);
                return;
            case 13:
                canvas.DrawImage(exp13, headX, headY, 71, 100);
                return;
            case 14:
                canvas.DrawImage(exp14, headX, headY, 71, 100);
                return;
            case 15:
                canvas.DrawImage(exp15, headX, headY, 71, 100);
                return;
            default:
                canvas.DrawImage(exp16, headX, headY, 71, 100);
                return;
        }
    }
}