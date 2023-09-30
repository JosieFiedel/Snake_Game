/// PS7 Method Implementation by Josie Fiedel & Braden Fiedel
/// November 11, 2022
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil;

public static class Networking
{
    /////////////////////////////////////////////////////////////////////////////////////////
    // Server-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
    /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
    /// AcceptNewClient will continue the event-loop.
    /// </summary>
    /// <param name="toCall">The method to call when a new connection is made</param>
    /// <param name="port">The the port to listen on</param>
    public static TcpListener StartServer(Action<SocketState> toCall, int port)
    {
        // Accepts connections on any of the machine's IP addresses, assuming the port is valid.
        TcpListener listener = new(IPAddress.Any, port);
        try
        { 
            listener.Start();
            // Begin accepting sockets as clients arrive in the buffer. 
            listener.BeginAcceptSocket(AcceptNewClient, Tuple.Create(toCall, listener));
        }
        catch { } // "Quietly" handle the error. 
        return listener;
    }

    /// <summary>
    /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
    /// continues an event-loop to accept additional clients.
    ///
    /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
    /// OnNetworkAction should be set to the delegate that was passed to StartServer.
    /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
    /// 
    /// If anything goes wrong during the connection process (such as the server being stopped externally), 
    /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccurred flag set to true 
    /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
    /// an error occurs.
    ///
    /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
    /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
    /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
    private static void AcceptNewClient(IAsyncResult ar)
    {
        // The OnNetworkAction delegate and the listener are extracted from the IAsyncResult.
        Tuple<Action<SocketState>, TcpListener> arTuple = (Tuple<Action<SocketState>, TcpListener>)ar.AsyncState!;
        Action<SocketState> toCall = arTuple.Item1;
        TcpListener listener = arTuple.Item2;

        try
        {
            Socket socket = listener.EndAcceptSocket(ar);
            SocketState state = new(toCall, socket);
            // Action is performed, as defined by the user's delegate. 
            state.OnNetworkAction(state);
            // If the connection process is successful, the 'accept socket' event loop continues.
            listener.BeginAcceptSocket(AcceptNewClient, arTuple);

        }
        // The user's delegate is invoked and a new SocketState is created with an error message.
        catch (Exception ex)
        {
            OnNetworkActionError(toCall, "Error occurred during the client connection process:\n" + ex.Message);
        }
    }

    /// <summary>
    /// Helper method for invoking the OnNetworkAction delegate. The OnNetworkAction delegate is invoked with 
    /// a new SocketState with its ErrorOccurred flag set to true and an appropriate message placed in its ErrorMessage 
    /// field.
    /// </summary>
    /// <param name="performNetworkAction"> The user's network action delegate </param>
    /// <param name="message"> Appropriate error message </param>
    private static void OnNetworkActionError(Action<SocketState> performNetworkAction, string message)
    {
        performNetworkAction(new SocketState(performNetworkAction, message)); 
    }

    /// <summary>
    /// Stops the given TcpListener.
    /// </summary>
    public static void StopServer(TcpListener listener)
    {
        try { listener.Stop(); }
        catch { } // "Quietly" handle the error. 
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    // Client-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Begins the asynchronous process of connecting to a server via BeginConnect, 
        /// and using ConnectedCallback as the method to finalize the connection once it's made.
        /// 
        /// If anything goes wrong during the connection process, toCall should be invoked 
        /// with a new SocketState with its ErrorOccurred flag set to true and an appropriate message 
        /// placed in its ErrorMessage field. Depending on when the error occurs, this should happen either
        /// in this method or in ConnectedCallback.
        ///
        /// This connection process should timeout and produce an error (as discussed above) 
        /// if a connection can't be established within 3 seconds of starting BeginConnect.
        /// 
        /// </summary>
        /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
        /// <param name="hostName">The server to connect to</param>
        /// <param name="port">The port on which the server is listening</param>
    public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
    {
        // Establish the remote endpoint for the socket.
        IPHostEntry ipHostInfo;
        IPAddress ipAddress = IPAddress.None;

        // Determine if the server address is a URL or an IP.
        try
        {
            ipHostInfo = Dns.GetHostEntry(hostName);
            bool foundIPV4 = false;
            foreach (IPAddress addr in ipHostInfo.AddressList)
                if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    foundIPV4 = true;
                    ipAddress = addr;
                    break;
                }
            // Didn't find any IPV4 addresses.
            // The user's delegate is invoked and a new SocketState is created with an error message.
            if (!foundIPV4)
            {
                OnNetworkActionError(toCall, "ERROR: No IPV4 address found.");
            }
        }
        catch
        {
            // See if the host name is a valid IP address.
            try
            {
                ipAddress = IPAddress.Parse(hostName);
            }
            // The user's delegate is invoked and a new SocketState is created with an error message.
            catch (Exception innerEx)
            {
                OnNetworkActionError(toCall, "ERROR: Host name is not a valid IP address:\n" + innerEx.Message);
            }
            return;
        }

        // Create a TCP/IP socket.
        Socket socket = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // This disables Nagle's algorithm (google if curious!)
        // Nagle's algorithm can cause problems for a latency-sensitive 
        // game like ours will be 
        socket.NoDelay = true;

        try
        {
            // If the connection cannot be established within 3 seconds, the socket closes and a timeout error occurs,
            // which is handled in ConnectedCallback.
            IAsyncResult result = socket.BeginConnect(ipAddress, port, ConnectedCallback, new SocketState(toCall, socket));
            if (!result.AsyncWaitHandle.WaitOne(3000, true))
                CloseSocket(socket);
        }
        // The user's delegate is invoked and a new SocketState is created with an error message.
        catch (Exception ex)
        {
            OnNetworkActionError(toCall, "Error occurred during the connection process:\n" + ex.Message);
        }
    }

    /// <summary>
    /// Safely closes the provided socket. 
    /// </summary>
    /// <param name="socket"></param>
    private static void CloseSocket(Socket socket)
    {
        try { socket.Shutdown(SocketShutdown.Both); }
        catch { } // "Quietly" handle the error. 
        socket.Close();
    }

    /// <summary>
    /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
    ///
    /// Uses EndConnect to finalize the connection.
    /// 
    /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
    /// either this method or ConnectToServer should indicate the error appropriately.
    /// 
    /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
    /// with a new SocketState representing the new connection.
    /// 
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginConnect</param>
    private static void ConnectedCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState)ar.AsyncState!;
        try
        {
            state.TheSocket.EndConnect(ar);
            // Invoke the toCall Action with a new SocketState, as instructed in the XML comment. 
            SocketState newState = new(state.OnNetworkAction, state.TheSocket);
            newState.OnNetworkAction(newState);
        }
        // The user's delegate is invoked and a new SocketState is created with an error message.
        catch (Exception ex) 
        { 
            OnNetworkActionError(state.OnNetworkAction, "Error occurred during the connection process:\n" + ex.Message); 
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////
    // Server and Client Common Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
    /// as the callback to finalize the receive and store data once it has arrived.
    /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
    /// 
    /// If anything goes wrong during the receive process, the SocketState's ErrorOccurred flag should 
    /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
    /// OnNetworkAction should be invoked. Depending on when the error occurs, this should happen either
    /// in this method or in ReceiveCallback.
    /// </summary>
    /// <param name="state">The SocketState to begin receiving</param>
    public static void GetData(SocketState state)
    {
        try
        {
            state.TheSocket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, ReceiveCallback, state);
        }
        // The user's delegate is invoked and a new SocketState is created with an error message.
        catch (Exception ex) 
        { 
            OnNetworkActionError(state.OnNetworkAction, "Error occurred during the receive process:\n" + ex.Message); 
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
    /// 
    /// Uses EndReceive to finalize the receive.
    ///
    /// As stated in the GetData documentation, if an error occurs during the receive process,
    /// either this method or GetData should indicate the error appropriately.
    /// 
    /// If data is successfully received:
    ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
    ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
    ///      string builder.
    ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
    /// </summary>
    /// <param name="ar"> 
    /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
    /// </param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState)ar.AsyncState!;
        try
        {
            int numBytes = state.TheSocket.EndReceive(ar);
            // The socket is closed if zero bytes are returned.
            if(numBytes == 0)
                state = new(state.OnNetworkAction, "ERROR: The socket is closed.");
            else
            {
                // A lock is necessary in case the SocketState's StringBuilder is modified or read
                // by methods within the SocketState class.
                lock (state.data)
                {
                    state.data.Append(Encoding.UTF8.GetString(state.buffer, 0, numBytes)); 
                }
            }
            // Action is performed, as defined by the user's delegate. 
            state.OnNetworkAction(state);
        }
        // The user's delegate is invoked and a new SocketState is created with an error message.
        catch (Exception ex) 
        { 
            OnNetworkActionError(state.OnNetworkAction, "Error occurred during the receive process:\n" + ex.Message); 
        }
    }

    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool Send(Socket socket, string data)
    {
        return SendData(socket, data, SendCallback);
    }

    /// <summary>
    /// Helper method for sending data. 
    /// Begins the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
    /// If the socket is closed, it does not attempt to send. If a send fails for any reason, this method ensures that
    /// the socket is closed before returning. 
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <param name="callback">The appropriate user callback delegate</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    private static bool SendData(Socket socket, string data, AsyncCallback callback)
    {
        try
        {
            // Extract the bytes from the data string to send to a connected socket. 
            byte[] messageBytes = Encoding.UTF8.GetBytes(data);
            if (messageBytes.Length != 0)
            {
                socket.BeginSend(messageBytes, 0, messageBytes.Length, SocketFlags.None, callback, socket);
                return true;
            }
        }
        catch 
        {
            CloseSocket(socket);
        }
        return false;
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by Send.
    ///
    /// Uses EndSend to finalize the send.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendCallback(IAsyncResult ar)
    {
        try { ((Socket)ar.AsyncState!).EndSend(ar); }
        catch { } // "Quietly" handle the error. 
    }


    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
    /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool SendAndClose(Socket socket, string data)
    {
        return SendData(socket, data, SendAndCloseCallback);
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
    ///
    /// Uses EndSend to finalize the send, then closes the socket.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// 
    /// This method ensures that the socket is closed before returning.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendAndCloseCallback(IAsyncResult ar)
    {
        Socket socket = (Socket)ar.AsyncState!;
        try { socket.EndSend(ar); }
        catch { } // "Quietly" handle the error.
        CloseSocket(socket);
    }
}