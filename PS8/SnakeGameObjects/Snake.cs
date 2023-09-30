/// PS8 / PS9 implementation by Josie Fiedel & Braden Fiedel
/// November 28, 2022
using Newtonsoft.Json;
using System.Drawing;

namespace SnakeGame;

/// <summary>
/// Represents a Snake object in the game world.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Snake
{
    public const int Radius = 5;                // Default snake radius. May be changed.

    [JsonProperty(PropertyName = "snake")]
    public long ID { get; private set; }        // Unique ID number. 

    [JsonProperty]
    public string name { get; private set; }    // Inputted player name.

    [JsonProperty]
    public List<Vector2D> body { get; set; }    // List of the snake's body segment locations.

    [JsonProperty]
    public Vector2D dir { get; set; }           // The snake's orientation.

    [JsonProperty]
    public int score { get; set; }              // The number of powerups hit.

    [JsonProperty]
    public bool died { get; set; }              // True if the snake died on this frame.

    [JsonProperty]
    public bool alive { get; set; }             // Dead or alive status.

    [JsonProperty]
    public bool dc { get; set;  }               // True if the player disconnected.

    [JsonProperty]
    public bool join { get; private set; }              // True if the player connected on this frame.

    public bool animationStatus;                        // True if the player's death animation is currently playing.

    public int animationFrame;                          // The current frame of the player's death animation. 

    public int respawnFrame;                            // The current frame of the player's respawn wait. 

    public int powerupFrame;                            // The current frame of the player picking up a powerup. 

    /// <summary>
    /// Constructor used for JSON deserialization. Sets the snake's fields to default values.
    /// </summary>
    public Snake()
    {
        ID = -1;
        name = "";
        body = new();
        dir = new();
        score = 0;
        died = false;
        alive = true;
        dc = false;
        join = true;
        animationStatus = false;
        animationFrame = 0;
        respawnFrame = 0;
        powerupFrame = 0;
    }

    public Snake(long ID, string name) : this()
    {
        this.ID = ID;
        this.name = name;
    }

    /// <summary>
    /// Sets the direction of the snake, given a vector. 
    /// </summary>
    /// <param name="vect"></param>
    public void SetDirection(Vector2D vect)
    {
        dir = vect;
    }
}