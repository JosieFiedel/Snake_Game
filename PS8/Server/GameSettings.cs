/// PS9 implementation by Braden Fiedel & Josie Fiedel
/// December 8, 2022
using System.Runtime.Serialization;

namespace SnakeGame;

/// <summary>
/// Contains fields for the hard-coded and modifyable server settings of the SNAKE game.
/// </summary>
[DataContract(Namespace = "")]
public class GameSettings
{
    // Hard-coded server items. 
    [DataMember]
    public int SnakeSpeed { get; private set; }
    [DataMember]
    public int SnakeStartLength { get; private set; }
    [DataMember]
    public int SnakeGrowth { get; private set; }
    [DataMember]
    public int MaxPowerups { get; set; }
    [DataMember]
    public int MaxPowerupDelay { get; private set; }

    // Modifiable server items. 
    [DataMember]
    public int FramesPerShot { get; private set; }
    [DataMember]
    public int MSPerFrame { get; private set; }
    [DataMember]
    public int UniverseSize { get; private set; }
    [DataMember]
    public int RespawnRate { get; private set; }
    [DataMember]
    public List<Wall> Walls { get; private set; }

    public GameSettings()
    {
        SnakeSpeed = 3;
        SnakeStartLength = 120;
        SnakeGrowth = 12;
        MaxPowerups = 20;
        MaxPowerupDelay = 200;

        FramesPerShot = -1;
        MSPerFrame = -1;
        UniverseSize = -1;
        RespawnRate = -1;
        Walls = new();
    }
}
