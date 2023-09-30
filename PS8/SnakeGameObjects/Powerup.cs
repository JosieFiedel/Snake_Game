/// PS8 implementation by Josie Fiedel & Braden Fiedel
/// November 28, 2022
using Newtonsoft.Json;

namespace SnakeGame;

/// <summary>
/// Represents a Powerup object in the game world.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Powerup
{
    public const int Width = 10;

    [JsonProperty(PropertyName = "power")]
    public int ID { get; private set; }         // Unique ID number. 

    [JsonProperty]
    public Vector2D loc { get; set; }           // Location in the world.

    [JsonProperty]
    public bool died { get; set; }              // Dead or alive status. 

    public Powerup(int ID) : this()
    {
        this.ID = ID;
    }

    /// <summary>
    /// Constructor used for JSON deserialization. Sets the powerup's fields to default values.
    /// </summary>
    public Powerup()
    {
        ID = -1;
        loc = new();
        died = false;
    }
}