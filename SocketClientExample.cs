using System;
using System.Net.Sockets;

using UnityEngine;

public class SocketClientExample : MonoBehaviour
{
    // TCP socket object.
    public TcpClient client;

    public void Start()
    {
        // Your connection data.
        string serverIP = "yourip";
        int serverPort = 12345;

        // Create a TCP client socket.
        this.client = new TcpClient();

        try
        {
            // Connect to the server.
            this.client.Connect(serverIP, serverPort);
            Debug.Log($"Connected to {serverIP}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }

    // Receive an array of floats from server.
    public float[] ReadMessageFromServer()
    {
        try
        {
            // Get the network stream from the client socket.
            NetworkStream stream = this.client.GetStream();

            // Receive a response from the server.
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            float[] responseFloats = new float[bytesRead / 4];

            // Copy the response into an array.
            for (int i = 0; i < responseFloats.Length; i++)
            {
                byte[] floatBytes = new byte[4];
                Array.Copy(buffer, i * 4, floatBytes, 0, 4);

                responseFloats[i] = BitConverter.ToSingle(floatBytes, 0);
            }

            return responseFloats;
        }
        catch (Exception e)
        {
            Debug.Log("READ MESSAGE ERROR: " + e.Message);

            return null;
        }
    }

    // Sends an array of floats to server.
    public void SendMessageToServer(float[] state)
    {
        try
        {
            // Get the network stream from the client socket.
            NetworkStream stream = this.client.GetStream();

            // Convert the message to bytes.
            byte[] data = new byte[state.Length * 4];
            for (int i = 0; i < state.Length; i++)
            {
                byte[] floatBytes = BitConverter.GetBytes(state[i]);

                Array.Copy(floatBytes, 0, data, i * 4, 4);
            }

            // Send the data to the server.
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.Log("SEND MESSAGE ERROR: " + e.Message);
        }
    }
}
