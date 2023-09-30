/// PS8 implementation by Josie Fiedel & Braden Fiedel
/// November 28, 2022

namespace SnakeGame;

/// <summary>
/// Contains the GUI code which creates the player's "view" of the game. Draws the
/// game objects, the players' names and scores, and the GUI controls (textboxes & buttons).
/// Includes event handlers for basic user inputs and controller events, which invoke
/// logic-based methods in the GameController class.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly GameController controller;  // Controller--handles the view's logic. 

    public MainPage()
    {
        InitializeComponent();
        controller = new();

        controller.Connected += HandleConnection;             // Server/client connection action.
        controller.UpdateReceived += HandleUpdate;            // Server message received action.
        controller.DimensionsReceived += HandleDrawMap;       // Dimensions received action.
        controller.PlayerConnected += HandlePlayerJoin;       // Player connect action.
        controller.PlayerDisconnected += HandlePlayerLeave;   // Player disconnect action.
        controller.NetworkError += HandleNetworkError;        // Network error action.
    }

    /// <summary>
    /// Invoked once a connection has been established. Sends the player name to the server
    /// to start the message event loop. Some GUI components are disabled to prevent 
    /// the user from attempting to connect elsewhere while already being connected to a server. 
    /// </summary>
    private void HandleConnection()
    {
        Dispatcher.Dispatch(() =>
        {
            nameText.IsEnabled = false;
            serverText.IsEnabled = false;
            connectButton.IsEnabled = false;
        });

        // Send the playername to GameController to send it to the server.
        controller.StartCommunicating(nameText.Text);
    }

    /// <summary>
    /// Event handler for when the controller has updated the world.
    /// The graphicsView is informed to redraw. 
    /// </summary>
    public void HandleUpdate()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Sets the world and the client's ID in the worldPanel. 
    /// </summary>
    private void HandleDrawMap()
    {
        worldPanel.SetWorldAndID(controller.theWorld, controller.ID);
    }

    /// <summary>
    /// Event handler for a player connecting to the server.
    /// Displays text to notify of the player's connection. 
    /// </summary>
    /// <param name="name"> Player name </param>
    private void HandlePlayerJoin(string name)
    {
        Dispatcher.Dispatch(() => playerConnect.Text = "[" + DateTime.Now.ToShortTimeString() + "]  " + name + " connected.");
    }

    /// <summary>
    /// Event handler for a player disconnecting from the server.
    /// Displays text to notify of the player's disconnection.
    /// </summary>
    /// <param name="name"> Player name </param>
    private void HandlePlayerLeave(string name)
    {
        Dispatcher.Dispatch(() => playerConnect.Text = "[" + DateTime.Now.ToShortTimeString() + "]  " + name + " disconnected.");
    }

    /// <summary>
    /// The typed characters 'w', 'a', 's', and 'd' are associated with player movement. 
    /// As these characters are entered into the textbox, the server is notified of the
    /// player movement. The textbox is automatically cleared once the text input is entered. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        string text = entry.Text.ToLower();

        controller.Move(text);
        entry.Text = "";
    }

    /// <summary>
    /// Event handler for errors that occur during the connection process. 
    /// All controller fields are reset to prepare for a new connection.
    /// </summary>
    /// <param name="error"> Error message </param>
    private void HandleNetworkError(string error)
    {
        Dispatcher.Dispatch(() =>
        {
            DisplayAlert("Error Occurred.", error, "Try again");
            nameText.IsEnabled = true;
            serverText.IsEnabled = true;
            connectButton.IsEnabled = true;
        });

        controller.ResetState();
    }

    /// <summary>
    /// Event handler for the connect button. If the player name or server address
    /// input is not valid, the user is notified of this. If both the player name
    /// and the server address are valid, the connection process begins. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
        
        controller.Connect(serverText.Text);
        keyboardHack.Focus();
    }

    /// <summary>
    /// Focuses to the textbox if the connect button is disabled, allowing
    /// player movement keyboard input. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }

    /// <summary>
    /// Sets focus to the entry textbox to take in keyboard inputs. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// When the Controls button is clicked, information about the controls is displayed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// When the About button is clicked, general information about the game is displayed. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation and art by Josie Fiedel & Braden Fiedel\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }
}