using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SocketClient2 : MonoBehaviour
{
    private const int port = 1234;
    private const string serverIP = "192.168.137.1";

    private Socket clientSocket;
    private byte[] receiveBuffer = new byte[2048]; // Increased buffer size to handle larger data

    public GameObject targetObject1;
    public GameObject targetObject2;

    private Vector3 newPosition1;
    private Quaternion newRotation1;

    private float messageInterval = 0.07f; // Interval in seconds between messages
    private float timeSinceLastMessage = 0f;

    void Start()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ConnectToServer();
    }

    void Update()
    {
        if (targetObject1 != null)
        {
            targetObject1.transform.position = targetObject1.transform.parent.TransformPoint(newPosition1);
            targetObject1.transform.rotation = targetObject1.transform.parent.rotation * newRotation1;
        }

        // Increment the time since the last message
        timeSinceLastMessage += Time.deltaTime;

        // Check if the interval has passed
        if (timeSinceLastMessage >= messageInterval)
        {
            // Send the position and rotation of targetObject2
            if (targetObject2 != null)
            {
                Vector3 position = targetObject2.transform.position;
                Quaternion rotation = targetObject2.transform.rotation;

                // Multiply each value of XYZ by 100 before sending
                float posX = position.x * -100;
                float posY = position.y * 100;
                float posZ = position.z * 100;

                SendMessageToServer($"Rover1,{posX},{posY},{posZ},{rotation.x},{rotation.y},{rotation.z},{rotation.w}");
            }

            // Reset the timer
            timeSinceLastMessage = 0f;
        }
    }

    private void ConnectToServer()
    {
        try
        {
            clientSocket.Connect(IPAddress.Parse(serverIP), port);
            clientSocket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.Log("Socket exception: " + e.ToString());
        }
    }

    private void ReceiveCallback(IAsyncResult AR)
    {
        try
        {
            int received = clientSocket.EndReceive(AR);
            if (received > 0)
            {
                byte[] data = new byte[received];
                Array.Copy(receiveBuffer, data, received);
                string message = Encoding.UTF8.GetString(data);

                // Remove {, }, (, ), [, ] from the message and split by commas
                message = message.Replace("{", "").Replace("}", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace("\'", "").Replace(",", "");
                string[] parts = message.Split(' ');

                // Debug log the parsed parts
                Debug.Log(message);

                // Parse XYZ data for one object
                if (parts.Length == 7)
                {
                    // Parse first object's position and rotation
                    float x1, y1, z1, rx1, ry1, rz1, w1;
                    if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x1) &&
                        float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y1) &&
                        float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z1) &&
                        float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rx1) &&
                        float.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ry1) &&
                        float.TryParse(parts[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rz1) &&
                        float.TryParse(parts[6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w1))
                    {
                        // Convert from cm to meters
                        x1 /= 100.0f;
                        y1 /= 100.0f;
                        z1 /= 100.0f;

                        // Update the new position and rotation for the first object
                        newPosition1 = new Vector3(-x1, y1, z1);

                        //newRotation1 = new Quaternion(rx1, ry1, rz1, w1);
                        Vector3 axis1 = new Vector3(rx1, ry1, rz1).normalized;
                        newRotation1 = Quaternion.AngleAxis(w1, axis1);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse first object's XYZW and quaternion data as float values!");
                    }

                }
                else
                {
                    Debug.LogError("Received message does not contain valid data for the object!");
                }
            }
            clientSocket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.Log("Receive callback exception: " + e.ToString());
        }
    }

    private void SendMessageToServer(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            clientSocket.Send(data);
        }
        catch (Exception e)
        {
            Debug.Log("Send message exception: " + e.ToString());
        }
    }

    private void OnDestroy()
    {
        if (clientSocket != null && clientSocket.Connected)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}
