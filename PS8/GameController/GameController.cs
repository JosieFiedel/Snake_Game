/// PS8 implementation by Josie Fiedel & Braden Fiedel
/// November 28, 2022
using NetworkUtil;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SnakeGame;

/// <summary>
/// Contains logic for parsing the data sent by the server, updating the 
/// model accordingly. The logic of inputs dealt with by the view (such as buttons,
/// textboxes, and key presses) is also handled here. 
/// </summary>
public class GameController
{
    // Fires when the server and client form a successful connection.
    public delegate void ConnectionHandler();
    public event ConnectionHandler? Connected;

    // Fires when the controller has received and processed new messages from the server.
    public delegate void GameUpdateHandler();
    public event GameUpdateHandler? UpdateReceived;

    // Fires when the world dimensions are received as a message from the server. 
    public delegate void WorldDimensionHandler();
    public event WorldDimensionHandler? DimensionsReceived;

    // Fires when a player connects to the server. 
    public delegate void PlayerConnectHandler(string name);
    public event PlayerConnectHandler? PlayerConnected;

    // Fires when a player disconnects from the server. 
    public delegate void PlayerDisconnectHandler(string name);
    public event PlayerDisconnectHandler? PlayerDisconnected;

    // Fires when a network error occurs. 
    public delegate void NetworkErrorHandler(string error);
    public event NetworkErrorHandler? NetworkError;


    private SocketState? theServer;               // Server--sends data to all the clients.
    public World? theWorld { get; private set; }  // World--container for the info of all objects.
    public int ID { get; private set; } = -1;     // The ID of the client's snake (-1 before it is set).


    /// <summary>
    /// Begins the process of connecting to the server. The default
    /// port to connect on is 11000.
    /// </summary>
    /// <param name="host"> User-provided host name </param>
    public void Connect(string host)
    {
        Networking.ConnectToServer(OnConnect, host, 11000);
    }

    /// <summary>
    /// "Action" to be performed once a connection is established. Stores
    /// the server SocketState and informs the view of the successful connection
    /// by the use of an event. 
    /// </summary>
    /// <param name="state"> The server SocketState </param>
    private void OnConnect(SocketState state)
    {
        // Check for network errors. 
        if (state.ErrorOccurred)
        {
            NetworkError?.Invoke(state.ErrorMessage!);
            return;
        }

        theServer = state;
        Connected?.Invoke();
    }

    /// <summary>
    /// Sends the client's player name to the server. Begins the event loop of
    /// requesting messages from the server. 
    /// </summary>
    /// <param name="playerName"> User-inputted playername </param>
    public void StartCommunicating(string playerName)
    {
        if (theServer != null)
        {
            Networking.Send(theServer.TheSocket, playerName + "\n");

            theServer.OnNetworkAction = ReceiveMessages;
            Networking.GetData(theServer);
        }
    }

    /// <summary>
    /// "Action" to be performed once the client receives messages from the server. 
    /// The messages are processed, the view is informed that the server sent messages,
    /// and the event loop of requesting messages from the server is continued.
    /// </summary>
    /// <param name="state"> SocketState containing the server messages </param>
    private void ReceiveMessages(SocketState state)
    {
        // Check for network errors. 
        if (state.ErrorOccurred)
        {
            NetworkError?.Invoke(state.ErrorMessage!);
            return;
        }

        // Process the messages sent by the server.
        ProcessMessages(state);
        if(theWorld != null)
            UpdateReceived?.Invoke();
        Networking.GetData(state);
    }

    /// <summary>
    /// Process the buffered messages, splitting by '\n'. The first server message corresponds to 
    /// the client's ID, the second server message corresponds to the world dimensions, and the 
    /// remaining messages represent the updated locations and states of server objects.
    /// </summary>
    /// <param name="state"> SocketState containing the server messages </param>
    private void ProcessMessages(SocketState state)
    {
        // Check for network errors. 
        if (state.ErrorOccurred)
        {
            NetworkError?.Invoke(state.ErrorMessage!);
            return;
        }

        string totalData = state.GetData();
        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        // Loop until all of the messages are processed.
        List<string> newMessages = new();
        foreach (string p in parts)
        {
            // Ignore empty strings.
            if (p.Length == 0)
                continue;

            // Ignore the last string if it does not end with '\n'.
            if (p[p.Length - 1] != '\n')
                break;

            // Build a list of messages to be deserialized. 
            newMessages.Add(p);
            // Remove the message from the SocketState's growable buffer.
            state.RemoveData(0, p.Length);
        }

        // Loop through each of the messages, adding or updating game objects and 
        // setting the world dimensions and client ID. 
        foreach(string message in newMessages)
        {
            // Adds or updates a snake in the world. 
            if (message.Contains("snake"))
            {
                Snake? snake = JsonConvert.DeserializeObject<Snake>(message);
                lock (theWorld!.snakes)
                {
                    // Inform the view of a player disconnection and remove the snake.
                    if (snake != null && snake.dc)   
                    {
                        PlayerDisconnected?.Invoke(snake.name!);
                        theWorld.snakes.Remove(snake.ID);
                    }
                    // Adds (or modifies) the snake in the World snakes dictionary.
                    // Preserves the old snake's death animation status. 
                    else
                    {
                        int frame = 0;
                        bool animationStatus = false;
                        if(theWorld!.snakes.TryGetValue(snake!.ID, out Snake? oldSnake))
                        {
                            frame = oldSnake.animationFrame;
                            animationStatus = oldSnake.animationStatus;
                        }
                        theWorld!.snakes[snake!.ID] = snake;
                        snake.animationFrame = frame;
                        snake.animationStatus = animationStatus;
                    }

                    // Inform the view of a player connection.
                    if (snake.join) 
                        PlayerConnected?.Invoke(snake.name!);
                }
            }
            // Adds or updates a powerup in the world.
            else if (message.Contains("power"))
            {
                Powerup? powerup = JsonConvert.DeserializeObject<Powerup>(message);
                lock (theWorld!.powerups)
                {
                    // Remove the powerup if it has been consumed by a snake.
                    if (powerup!.died)
                        theWorld.powerups.Remove(powerup.ID);
                    // Adds (or modifies) the powerup in the World powerups dictionary.
                    else
                        theWorld!.powerups[powerup.ID] = powerup;
                }
            }
            // Adds a wall to the world.
            else if (message.Contains("wall"))
            {
                Wall? newWall = JsonConvert.DeserializeObject<Wall>(message);
                lock (theWorld!.walls)
                {
                    // Adds the wall to the World walls dictionary.
                    theWorld!.walls[newWall!.ID] = newWall;
                }
            }
            // The player's ID is set with the first message sent by the server.
            else if (ID == -1)
            {
                ID = int.Parse(message);
            }
            // The world is created with the dimensions sent as the server's second message.
            else if (theWorld == null) 
            {
                theWorld = new(int.Parse(message));
                DimensionsReceived?.Invoke();
            }
        }
    }

    /// <summary>
    /// Retrieves the server error message.
    /// </summary>
    /// <returns> The error message </returns>
    public string? GetErrorMessage()
    {
        if(theServer != null)
            return theServer.ErrorMessage;
        return "ERROR: Server is null.";
    }

    /// <summary>
    /// Resets all controller fields. Called when an error occurs and a reconnection must be made.
    /// </summary>
    public void ResetState()
    {
        theServer = null;
        theWorld = null;
        ID = -1;
    }

    /// <summary>
    /// The logic behind the GUI key inputs responsible for a change in player movement. The client 
    /// may not send any movement requests until it receives information about the client's ID, world 
    /// size, and walls. In other words, no movement action may occur until there are Snake or Powerup 
    /// objects stored in the world dictionaries. 
    /// </summary>
    /// <param name="text"> The client's keyboard input </param>
    public void Move(string text)
    {
        // Movement occurs when there are Snake / Powerup objects in the world dictionaries. 
        if (theServer != null && (theWorld!.snakes.Any() || theWorld!.powerups.Any()))
        {
            // Move up
            if (text == "w")
                Networking.Send(theServer.TheSocket, "{\"moving\":\"up\"}\n");
            // Move left
            else if (text == "a")
                Networking.Send(theServer.TheSocket, "{\"moving\":\"left\"}\n");
            // Move down
            else if (text == "s")
                Networking.Send(theServer.TheSocket, "{\"moving\":\"down\"}\n");
            // Move right
            else if (text == "d")
                Networking.Send(theServer.TheSocket, "{\"moving\":\"right\"}\n");
        }
    }
}