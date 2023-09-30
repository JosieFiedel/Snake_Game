/// PS9 implementation by Braden Fiedel & Josie Fiedel
/// December 8, 2022
using NetworkUtil;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace SnakeGame;

/// <summary>
/// Computes game state information and relays it to all connected clients.
/// </summary>
public class Server
{
    private readonly World world;                            // Stores world objects.
    private readonly GameSettings settings;                  // Stores default game settings.
    private readonly Dictionary<long, SocketState> clients;  // Stores Current connections.

    /// <summary>
    /// Initializes a new instance of the Server class.
    /// </summary>
    /// <param name="gameSettings">An object representing the rules and setttings of the game.</param>
    private Server(GameSettings gameSettings)
    {
        settings = gameSettings;
        clients = new();
        world = new(settings.UniverseSize, settings.Walls, settings.MaxPowerupDelay);
    }

    /// <summary>
    /// When run, the server program starts here. The game settings file is read/stored and an instance of the
    /// server class is initialized using this data. This server then begins accepting connections and
    /// and the frame update loop is started.  
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        // Read and store the data of the game settings file. 
        XmlReader reader = XmlReader.Create("settings.xml");
        DataContractSerializer ser = new(typeof(GameSettings));
        GameSettings gameSettings = (GameSettings)ser.ReadObject(reader)!;

        // Initialize the server and begin accepting connections.
        Server server = new(gameSettings);
        Networking.StartServer(server.OnConnect, 11000);

        // Start the frame update loop. 
        server.Run();
    }

    /// <summary>
    /// Updates and sends game objects every through the use of a stopwatch.
    /// </summary>
    public void Run()
    {
        System.Diagnostics.Stopwatch watch = new();
        watch.Start();

        while (true)
        {
            // Wait until the next frame.
            while (watch.ElapsedMilliseconds < settings.MSPerFrame)
            { /* empty loop body */ }

            watch.Restart();

            // Send the state of the snake and powerup objects to all clients.  
            lock (clients)
            {
                foreach (SocketState ss in clients.Values)
                {
                    Socket clientSocket = ss.TheSocket;
                    lock (world.snakes)
                    {
                        foreach (Snake snake in world.snakes.Values)
                            Networking.Send(clientSocket, JsonConvert.SerializeObject(snake) + "\n");
                    }
                    lock(world.powerups)
                    {
                        foreach (Powerup powerup in world.powerups.Values)
                            Networking.Send(clientSocket, JsonConvert.SerializeObject(powerup) + "\n");
                    }
                }
            }

            UpdateWorld();
        }
    }

    /// <summary>
    /// Action to be performed once the server successfully starts and a client
    /// has connected. Requests data from the client, specifically their name.
    /// Once data arrives, ReceiveName is called.
    /// </summary>
    /// <param name="state">The client socket state.</param>
    private void OnConnect(SocketState state)
    {
        state.OnNetworkAction = ReceiveName;
        Networking.GetData(state);
    }

    /// <summary>
    /// Action to be performed once a client has sent their player name. A new snake object
    /// is created for the client. They are then sent their ID as well as the world dimensions
    /// and wall objects. Once more data is recieved, ReceiveCommand is called.
    /// </summary>
    /// <param name="state">The client socket state.</param>
    private void ReceiveName(SocketState state)
    {
        // Do not continue if an error has occured.
        if (state.ErrorOccurred)
            return;

        // Parse the data from the client's socket state. 
        string[] data = Regex.Split(state.GetData(), @"(?<=[\n])");
        string playerName = data[0][..^1];
        state.RemoveData(0, state.GetData().Length);

        // Create a new snake object using the provided player name. Add it to the world.
        Snake clientSnake = new(state.ID, playerName);
        lock(world.snakes)
        {
            SpawnSnake(clientSnake);
            world.snakes.Add(state.ID, clientSnake);
        }

        state.OnNetworkAction = ReceiveCommand;

        // Send the client their ID and the world dimensions.
        Networking.Send(state.TheSocket, state.ID + "\n" + settings.UniverseSize + "\n");

        // Send the client the wall objects.
        foreach (Wall wall in settings.Walls)
            Networking.Send(state.TheSocket, JsonConvert.SerializeObject(wall) + "\n");

        // Add the new client to the client dictionary.
        lock (clients)
        {
            clients[state.ID] = state;
        }

        Networking.GetData(state);
    }

    /// <summary>
    /// Spawns a client's snake with a randomized direction and location. 
    /// Collision detection is utilized to ensure that the snake does not
    /// spawn on another game object.
    /// </summary>
    /// <param name="state">The client socket state.</param>
    private void SpawnSnake(Snake snake)
    {
        Random rand = new();
        int halfWorldSize = settings.UniverseSize / 2;

        // Choose a random direction for the snake.
        List<Vector2D> possibleDirs = new() { new(1, 0), new(0, -1), new(-1, 0), new(0, 1) };
        Vector2D dir = possibleDirs[rand.Next(0, 4)];

        // Calculate a random location in the world for the snake tail.
        List<Vector2D> body = new() { new(rand.Next(-halfWorldSize, halfWorldSize), rand.Next(-halfWorldSize, halfWorldSize)) };

        // Place the head at an appropriate distance from the tail.
        body.Add(body.First() + (dir * settings.SnakeStartLength));

        // If either the snake body intersects any other game objects or it passes the world edge,
        // its position and direction are recalculated.
        while (DeathCollision(body, snake.ID, Snake.Radius) || PowerupCollision(body, dir) ||
            Math.Abs(body.Last().X) > halfWorldSize - 10 || Math.Abs(body.Last().Y) > halfWorldSize - 10 ||
            Math.Abs(body.First().X) > halfWorldSize - 10 || Math.Abs(body.First().Y) > halfWorldSize - 10)
        {
            dir = possibleDirs[rand.Next(0, 4)];
            body = new() { new(rand.Next(-halfWorldSize, halfWorldSize), rand.Next(-halfWorldSize, halfWorldSize)) };
            body.Add(body.First() + dir * settings.SnakeStartLength);
        }

        // Set the calculated data.
        snake.body = body;
        snake.dir = dir;
        snake.respawnFrame = 0;
    }

    /// <summary>
    /// Detects if an object collides with a wall or a snake.
    /// </summary>
    /// <param name="points">2 points representing the object's location in the world.</param>
    /// <param name="ID">An ID representing the object.</param>
    /// <param name="objRad">The radius of the object.</param>
    /// <returns>True if an object collides with a wall or a snake and false otherwise.</returns>
    private bool DeathCollision(List<Vector2D> points, long ID, int objRad)
    {
        // Calculate the top left coordinate and bottom right coordinate of the object to
        // find its rectangle boundary.
        double objMinX = Math.Min(points[0].X, points[1].X) - objRad;
        double objMaxX = Math.Max(points[0].X, points[1].X) + objRad;
        double objMinY = Math.Min(points[0].Y, points[1].Y) - objRad;
        double objMaxY = Math.Max(points[0].Y, points[1].Y) + objRad;

        // Compare every wall to the object rectangle.
        foreach (Wall wall in world.walls.Values)
        {
            // Calculate the top left coordinate and bottom right coordinate of the wall
            // to find its rectangle boundary.
            double wallMinX = Math.Min(wall.p1.X, wall.p2.X) - Wall.Width / 2;
            double wallMaxX = Math.Max(wall.p1.X, wall.p2.X) + Wall.Width / 2;
            double wallMinY = Math.Min(wall.p1.Y, wall.p2.Y) - Wall.Width / 2;
            double wallMaxY = Math.Max(wall.p1.Y, wall.p2.Y) + Wall.Width / 2;

            // Check for overlap between the object rectange and wall rectangle.
            if (wallMinX < objMaxX && wallMaxX > objMinX && wallMinY < objMaxY && wallMaxY > objMinY)
                return true;
        }

        // Compare every snake to the object rectangle. 
        foreach (Snake snakeOp in world.snakes.Values)
        {
            // If the object is a snake, do not compare with itself. 
            if (snakeOp.ID == ID || !snakeOp.alive)
                continue;

            // Compare every snake body segment to the object rectangle.
            Vector2D prevBodyPt = snakeOp.body[0];
            for (int i = 1; i < snakeOp.body.Count; i++)
            {
                // Calculate the top left coordinate and bottom right coordinate of the snake body segment.
                double snakeOpMinX = Math.Min(prevBodyPt.X, snakeOp.body[i].X) - Snake.Radius;
                double snakeOpMaxX = Math.Max(prevBodyPt.X, snakeOp.body[i].X) + Snake.Radius;
                double snakeMinOpY = Math.Min(prevBodyPt.Y, snakeOp.body[i].Y) - Snake.Radius;
                double snakeMaxOpY = Math.Max(prevBodyPt.Y, snakeOp.body[i].Y) + Snake.Radius;

                // Check for overlap between the object rectangle and the snake body segment rectangle.
                if (snakeOpMinX < objMaxX && snakeOpMaxX > objMinX && snakeMinOpY < objMaxY && snakeMaxOpY > objMinY)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Detects if a spawning snake collides with a powerup object by checking a sufficient number of points along the snake body. 
    /// </summary>
    /// <param name="body">2 points representing the spawning snake's body.</param>
    /// <param name="dir">The direction of the spawning snake.</param>
    /// <returns>True if the snake is spawning on top of a powerup and false otherwise.</returns>
    private bool PowerupCollision(List<Vector2D> body, Vector2D dir)
    {
        int pointChecks = settings.SnakeStartLength / Powerup.Width;

        // Compare every powerup to the spawning snake.
        foreach (Powerup powerup in world.powerups.Values)
        {
            // Check a sufficient number of points along the snake body.
            Vector2D currentPt = body.First();
            for (int i = 0; i < pointChecks; i++)
            {
                // Check for overlap between the spawning snake and the powerup.
                if ((Powerup.Width + Snake.Radius) > (powerup.loc - currentPt).Length())
                    return true;
                // Move to the next checking location. 
                currentPt += dir * Powerup.Width;
            }
        }
        return false;
    }

    /// <summary>
    /// Action to be performed when the client sends command information to the server.
    /// The data is only processed if the client's snake is alive. 
    /// </summary>
    /// <param name="state">The client socket state.</param>
    private void ReceiveCommand(SocketState state)
    {
        // If an error has occurred, remove the client from the client list and
        // set their snakes fields to the appropriate values.
        if (state.ErrorOccurred)
        {
            lock(clients)
            {
                clients.Remove(state.ID);
            }

            lock(world.snakes)
            {
                world.snakes[state.ID].dc = true;
                world.snakes[state.ID].died = true;
                world.snakes[state.ID].alive = false;
            }
            return;
        }

        // Respond to the command accordingly.
        ProcessMovement(state);
        Networking.GetData(state);
    }

    /// <summary>
    /// Processes the movement command sent by the client, as stored in the SocketState's
    /// byte array. A snake 180 check is also performed. If a client sends a command that
    /// would cause a snake to die from a quick 180 maneuver, then this command is ignored
    /// until it is safe. 
    /// </summary>
    /// <param name="state"></param>
    private void ProcessMovement(SocketState state)
    {
        Snake clientSnake;
        lock(world.snakes)
        {
            clientSnake = world.snakes[state.ID];
            // Movements should not be processed if the snake is dead. 
            if (!clientSnake.alive)
                return;
        }

        // Parse the data from the client's socket state. 
        string[] data = Regex.Split(state.GetData(), @"(?<=[\n])");
        string dir = data[0][..^1];
        state.RemoveData(0, state.GetData().Length);

        // Determine the direction from the client's command. 
        Vector2D? newDir = null;
        switch (dir)
        {
            case "{\"moving\":\"up\"}":
                newDir = new(0, -1);
                break;
            case "{\"moving\":\"left\"}":
                newDir = new(-1, 0);
                break;
            case "{\"moving\":\"down\"}":
                newDir = new(0, 1);
                break;
            case "{\"moving\":\"right\"}":
                newDir = new(1, 0);
                break;
        }

        // Check for snake object 180s. 
        if(clientSnake.body.Count >= 3)
        {
            Vector2D seg = clientSnake.body[^2] - clientSnake.body[^3];
            seg.Normalize();

            // The command is ignored if the snake is at risk of killing itself
            // by quickly turning around. 
            if (newDir != null && newDir.IsOppositeCardinalDirection(seg) &&
                (clientSnake.body[^2] - clientSnake.body[^1]).Length() <= Snake.Radius * 2)
                return;
        }
        // The snake may not move in the opposite direction it's currently moving. 
        if (newDir != null && !clientSnake.dir.IsOppositeCardinalDirection(newDir))
            clientSnake.dir = newDir;
    }

    /// <summary>
    /// Updates all world objects. If a snake has disconnected, it is removed from the world
    /// snake list. If the snake is not alive, then it is respawned on the appropriate
    /// respawn frame (RespawnRate in the GameSettings project). Powerups are also handled
    /// if a snake collides with it. The snake is then separately updated, which is explained
    /// in the method below. 
    /// </summary>
    private void UpdateWorld()
    {
        lock(world.snakes)
        {
            // Update the state and position of the snake objects.
            foreach (Snake snake in world.snakes.Values)
            {
                // If the client has disconnected, the snake is removed from the list and will
                // stop being sent to the other clients. 
                if (snake.dc)
                {
                    world.snakes.Remove(snake.ID);
                    continue;
                }

                // The snake is only dead on one frame.
                if (snake.died)
                    snake.died = false;

                // If the snake isn't alive, it is respawned after the respawn rate passes. 
                if (!snake.alive)
                {
                    if (snake.respawnFrame++ != settings.RespawnRate)
                        continue;
                    snake.alive = true;
                    SpawnSnake(snake);
                }

                Vector2D headSeg = snake.body.Last();
                lock(world.powerups)
                {
                    foreach (Powerup powerup in world.powerups.Values)
                    {
                        // If the powerup has been hit by the snake, it's set to dead and the
                        // snake's score is incremented. The snake's score is increased. 
                        if (!powerup.died && (Powerup.Width / 2 + Snake.Radius) > (powerup.loc - headSeg).Length())
                        {
                            powerup.died = true;
                            world.deadPowerups!.Enqueue(powerup);
                            snake.score++;
                            snake.powerupFrame = 1;
                        }
                    }
                }
                UpdateSnake(snake);
            }
        }
        lock(world.powerups)
        {
            SpawnPowerups();
        }
    }

    /// <summary>
    /// Updates the snake's length and position, depending on the velocity and whether or not
    /// a powerup has been consumed. Snake wrap around is also accounted for, where the snake is
    /// able to cross the world boundary and appear at the other side. 
    /// </summary>
    /// <param name="snake"> The snake to be updated </param>
    private void UpdateSnake(Snake snake)
    {
        Vector2D velocity = snake.dir * settings.SnakeSpeed;
        Vector2D headSeg = snake.body.Last();
        Vector2D neckSeg = snake.body[^2];

        // Determine the direction in which the snake is moving to add to the head. 
        if ((headSeg.X >= neckSeg.X && headSeg.Y == neckSeg.Y && !snake.dir.Equals(new Vector2D(1, 0))) ||
            (headSeg.X == neckSeg.X && headSeg.Y > neckSeg.Y && !snake.dir.Equals(new Vector2D(0, 1))) ||
            (headSeg.X < neckSeg.X && headSeg.Y == neckSeg.Y && !snake.dir.Equals(new Vector2D(-1, 0))) ||
            (headSeg.X == neckSeg.X && headSeg.Y <= neckSeg.Y && !snake.dir.Equals(new Vector2D(0, -1))))
            snake.body.Add(headSeg + velocity);
        else
            snake.body[^1] = headSeg + velocity;

        // Once the powerup collection delay is over, reset the powerup frame. 
        if (snake.powerupFrame >= settings.SnakeGrowth)
            snake.powerupFrame = 0;

        // If the powerup has been recently collected, the powerup frame is incremented. 
        else if (snake.powerupFrame > 0)
            snake.powerupFrame++;

        // The tail grows if a powerup has not been recently collected. 
        else if (snake.powerupFrame == 0)
        {
            // Remove from the tail.
            Vector2D tailSeg = snake.body.First();
            Vector2D buttSeg = snake.body[1];
            Vector2D tailDir = tailSeg - buttSeg;
            tailDir.Normalize();

            snake.body[0] = new(tailSeg - (tailDir * settings.SnakeSpeed));

            // If the position of the tail vertex is equal to the position of the butt vertex,
            // delete the old tail vertex and set it to the next vertex.
            if (tailSeg.Equals(buttSeg))
                snake.body = snake.body.GetRange(1, snake.body.Count - 1);
        }

        // Check for object collisions that would cause the snake to die.
        // If the snake dies, its death status is changed and its score is reset. 
        if (DeathCollision(new List<Vector2D>() { neckSeg, headSeg }, snake.ID, Snake.Radius) || SelfCollision(snake))
        {
            snake.died = true;
            snake.score = 0;
            snake.alive = false;
        }
    }

    /// <summary>
    /// Determines if a self-collision has occured, i.e. the snake object runs into itself.
    /// Snake zig zags and other collisions are accounted for. All snake segments (pairs of snake vectors)
    /// are iterated through and their directions are determined. If one of the segments is
    /// opposite to the direction of the head, then this is where the collision detection starts.
    /// The head point is compared to all remaining segments behind this segment, and if they
    /// happen to overlap with an added offset of the snake's width, then a collision occurs. 
    /// </summary>
    /// <param name="snake"> The snake object to be checked for collisions </param>
    /// <returns> True if a collision has occurred, false otherwise </returns>
    private static bool SelfCollision(Snake snake)
    {
        // Top left and bottom right coords of the snake object's head and neck segment. 
        double headMinX = Math.Min(snake.body.Last().X, snake.body[^2].X) - Snake.Radius;
        double headMaxX = Math.Max(snake.body.Last().X, snake.body[^2].X) + Snake.Radius;
        double headMinY = Math.Min(snake.body.Last().Y, snake.body[^2].Y) - Snake.Radius;
        double headMaxY = Math.Max(snake.body.Last().Y, snake.body[^2].Y) + Snake.Radius;

        bool collisionCheck = false;
        Vector2D previousPt = snake.body.Last();
        // Loop through the snake segments to check for a possibility of self collisions. 
        for (int i = snake.body.Count - 1; i >= 0; i--)
        {
            // Determine the vector direction of the snake segment. 
            Vector2D seg = previousPt - snake.body[i];
            seg.Normalize();

            // If the two segments are opposite directions, then collisions become possible
            // and accounted for. 
            if (snake.dir.IsOppositeCardinalDirection(seg) && !collisionCheck)
                collisionCheck = true;

            // Checks for collisions with a rectangle overlap-based strategy. 
            if (collisionCheck)
            {
                // The top left and bottom right coords of the segment.
                double segMinX = Math.Min(snake.body[i].X, previousPt.X) - Snake.Radius;
                double segMaxX = Math.Max(snake.body[i].X, previousPt.X) + Snake.Radius;
                double segMinY = Math.Min(snake.body[i].Y, previousPt.Y) - Snake.Radius;
                double segMaxY = Math.Max(snake.body[i].Y, previousPt.Y) + Snake.Radius;

                if (headMinX < segMaxX && headMaxX > segMinX && headMinY < segMaxY && headMaxY > segMinY)
                    return true;
            }
            previousPt = snake.body[i]; 
        }
        return false;
    }

    /// <summary>
    /// Spawns in powerup objects when the necessary conditions are met. There must only be a certain
    /// number of powerups in the world at one time, given by the MaxPowerups field in the GameSettings
    /// class. Also, the powerups are spawned a random amount of time apart from each other. If the max
    /// number of powerups are alive in the world, then no more powerups are spawned, even if the amount
    /// of time is reached. 
    /// </summary>
    private void SpawnPowerups()
    {
        // Spawn in world powerups if the max powerup number isn't reached and the powerup delay frame is hit. 
        if ((world.deadPowerups!.Count > 0 || world.powerups.Count < settings.MaxPowerups) &&
            world.currentPowerupFrame >= world.maxPowerupFrame)
        {
            // Reset the current frame and adjust the max powerup spawn frame to be randomized. 
            Random rand = new();
            world.currentPowerupFrame = 0;
            world.maxPowerupFrame = rand.Next(0, settings.MaxPowerupDelay);

            // If the powerup dictionary is not full, add to the dictionary. 
            Powerup powerup;
            if (world.powerups.Count < settings.MaxPowerups)
            {
                powerup = new(world.powerups.Count + 1);
                world.powerups.Add(world.powerups.Count + 1, powerup);
            }
            // If the queue is not empty, the dead powerups are respawned. 
            else
            {
                powerup = world.deadPowerups.Dequeue();
                world.powerups[powerup.ID].died = false;
            }

            // Determine an appropriate random location for the powerup. 
            int halfWorldSize = settings.UniverseSize / 2;
            powerup.loc = new(rand.Next(-halfWorldSize, halfWorldSize), rand.Next(-halfWorldSize, halfWorldSize));

            // The Powerup's center point is passed in twice since the DeathCollision also deals with rectangle-shaped objects.
            while (DeathCollision(new() { new(powerup.loc.X, powerup.loc.Y), new(powerup.loc.X, powerup.loc.Y) }, -1, Powerup.Width / 2) 
                || PowerupCollision(powerup))
                powerup.loc = new(rand.Next(-halfWorldSize, halfWorldSize), rand.Next(-halfWorldSize, halfWorldSize));
        }
        else
            world.currentPowerupFrame++;
    }

    /// <summary>
    /// Determines if the passed in powerup collides with any of the other powerups
    /// in the dictionary. This method is primarily used for the spawning of
    /// powerpoint objects to ensure that a powerup does not spawn on another powerup
    /// object. 
    /// </summary>
    /// <param name="pow"> The powerup object to be spawned </param>
    /// <returns> True if it collides, false otherwise </returns>
    private bool PowerupCollision(Powerup pow)
    {
        foreach(Powerup otherPow in world.powerups.Values)
        {
            // Ignore the powerup if it matches this powerup's ID. 
            if (otherPow.ID == pow.ID)
                continue;
            // If the distance from the powerup's is less than the
            // powerup's width, then a collision occurred. 
            if ((otherPow.loc - pow.loc).Length() <= Powerup.Width)
                return true;
        }
        return false;
    }
}