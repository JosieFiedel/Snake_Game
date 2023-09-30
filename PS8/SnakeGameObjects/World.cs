/// PS8 / PS9 implementation by Josie Fiedel & Braden Fiedel
/// November 28, 2022
namespace SnakeGame;

/// <summary>
/// Represents the game world. A container holding all of the game
/// objects (snakes, powerups, and walls). The size of the world is
/// sent as a message by the server. 
/// </summary>
public class World
{
    public Dictionary<long, Snake> snakes;      // Contains all world Snake objects.
    public Dictionary<int, Powerup> powerups;   // Contains all world Powerup objects.
    public Queue<Powerup>? deadPowerups;        // Contains all dead Powerup objects.
    public Dictionary<int, Wall> walls;         // Contains all world Wall objects.

    public int size { get; private set; }       // World dimensions (size x size).
    public int maxPowerupFrame;                 // The maximum time for a powerup to spawn.
    public int currentPowerupFrame;             // Current wait for a powerup to spawn. 

    /// <summary>
    /// Initializes the dimensions of the game world and dictionaries representing 
    /// objects in the world. 
    /// </summary>
    /// <param name="size"> The world dimensions </param>
    public World(int size, List<Wall> walls, int powerupDelay) : this(size)
    {
        this.walls = walls.ToDictionary(x => x.ID, x => x);
        maxPowerupFrame = powerupDelay;
        currentPowerupFrame = 0;
        deadPowerups = new();
    }

    public World(int size)
    {
        snakes = new();
        powerups = new();
        deadPowerups = null;
        this.size = size;
        walls = new();
    }
}