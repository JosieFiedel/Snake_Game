/// PS8 implementation by Josie Fiedel & Braden Fiedel
/// November 28, 2022
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace SnakeGame;

/// <summary>
/// Represents a Wall object in the game world.
/// </summary>
/// 
[DataContract(Namespace = "")]
[JsonObject(MemberSerialization.OptIn)]
public class Wall
{
    public const int Width = 50;                // Default width of a wall. Can be changed. 

    [DataMember]
    [JsonProperty(PropertyName = "wall")]
    public int ID { get; private set; }         // Unique ID number.

    [DataMember]
    [JsonProperty]
    public Vector2D p1 { get; private set; }   // Center of the first wall endpoint.

    [DataMember]
    [JsonProperty]
    public Vector2D p2 { get; private set; }   // Center of the second wall endpoint.

    /// <summary>
    /// Constructor used for JSON deserialization. Sets the walls's fields to default values.
    /// </summary>
    public Wall()
    {
        ID = -1; 
        p1 = new();
        p2 = new();
    }
}