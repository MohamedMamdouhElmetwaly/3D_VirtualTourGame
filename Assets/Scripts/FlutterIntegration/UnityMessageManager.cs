using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages two-way communication between Unity and Flutter.
/// Handles sending messages to Flutter and receiving messages from Flutter.
/// </summary>
public class UnityMessageManager : MonoBehaviour
{
    private static UnityMessageManager instance;
    
    // Dictionary to store message handlers by message type
    private Dictionary<string, Action<string>> messageHandlers = new Dictionary<string, Action<string>>();

    // Event fired when any message is received
    public event Action<string, string> OnMessageReceived;

    /// <summary>
    /// Singleton instance of UnityMessageManager.
    /// </summary>
    public static UnityMessageManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UnityMessageManager");
                instance = go.AddComponent<UnityMessageManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        // Accessing the Instance property automatically instantiates and initializes the manager.
        var unused = Instance;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sends a message to Flutter.
    /// </summary>
    /// <param name="messageType">The type/category of the message</param>
    /// <param name="data">The message data as a string (can be JSON)</param>
    public void SendMessageToFlutter(string messageType, string data)
    {
        string jsonMessage = JsonUtility.ToJson(new MessageData
        {
            type = messageType,
            data = data
        });

        // Use the new SendToFlutter helper for package compatibility
        SendToFlutter.Send(jsonMessage);
    }

    /// <summary>
    /// Receives a message from Flutter. This method is called from the Flutter side.
    /// </summary>
    /// <param name="message">JSON string containing message type and data</param>
    public void ReceiveMessageFromFlutter(string message)
    {
        try
        {
            MessageData messageData = JsonUtility.FromJson<MessageData>(message);
            Debug.Log($"[Flutter->Unity] Received: {messageData.type} | {messageData.data}");

            // Fire the general event
            OnMessageReceived?.Invoke(messageData.type, messageData.data);

            // Call specific handler if registered
            if (messageHandlers.ContainsKey(messageData.type))
            {
                messageHandlers[messageData.type]?.Invoke(messageData.data);
            }
            else
            {
                Debug.LogWarning($"No handler registered for message type: {messageData.type}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse message from Flutter: {e.Message}");
        }
    }

    /// <summary>
    /// Registers a handler for a specific message type.
    /// </summary>
    /// <param name="messageType">The message type to listen for</param>
    /// <param name="handler">The callback function to execute when this message type is received</param>
    public void RegisterMessageHandler(string messageType, Action<string> handler)
    {
        if (messageHandlers.ContainsKey(messageType))
        {
            messageHandlers[messageType] += handler;
        }
        else
        {
            messageHandlers[messageType] = handler;
        }
        Debug.Log($"Registered handler for message type: {messageType}");
    }

    /// <summary>
    /// Unregisters a handler for a specific message type.
    /// </summary>
    /// <param name="messageType">The message type to stop listening for</param>
    /// <param name="handler">The callback function to remove</param>
    public void UnregisterMessageHandler(string messageType, Action<string> handler)
    {
        if (messageHandlers.ContainsKey(messageType))
        {
            messageHandlers[messageType] -= handler;
            if (messageHandlers[messageType] == null)
            {
                messageHandlers.Remove(messageType);
            }
            Debug.Log($"Unregistered handler for message type: {messageType}");
        }
    }

    /// <summary>
    /// Quits the Unity application. Useful when closing from Flutter.
    /// </summary>
    public void QuitUnity()
    {
        Debug.Log("Unity application quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Pauses the Unity application.
    /// </summary>
    public void PauseUnity()
    {
        Time.timeScale = 0;
        AudioListener.pause = true;
        Debug.Log("Unity paused");
    }

    /// <summary>
    /// Resumes the Unity application.
    /// </summary>
    public void ResumeUnity()
    {
        Time.timeScale = 1;
        AudioListener.pause = false;
        Debug.Log("Unity resumed");
    }

    /// <summary>
    /// Data structure for Unity-Flutter messages.
    /// </summary>
    [Serializable]
    private class MessageData
    {
        public string type;
        public string data;
    }
}

/// <summary>
/// Example usage component - demonstrates how to use UnityMessageManager.
/// Attach this to a GameObject to test the messaging system.
/// </summary>
public class UnityMessageManagerExample : MonoBehaviour
{
    private void Start()
    {
        // Register handlers for specific message types
        UnityMessageManager.Instance.RegisterMessageHandler("SET_CAMERA_SPEED", OnSetCameraSpeed);
        UnityMessageManager.Instance.RegisterMessageHandler("RESET_POSITION", OnResetPosition);
        UnityMessageManager.Instance.RegisterMessageHandler("TOGGLE_CONTROLS", OnToggleControls);

        // Listen to all messages
        UnityMessageManager.Instance.OnMessageReceived += OnAnyMessageReceived;

        // Send a ready message to Flutter
        UnityMessageManager.Instance.SendMessageToFlutter("UNITY_READY", "Unity scene loaded successfully");
    }

    private void OnDestroy()
    {
        // Clean up handlers
        UnityMessageManager.Instance.UnregisterMessageHandler("SET_CAMERA_SPEED", OnSetCameraSpeed);
        UnityMessageManager.Instance.UnregisterMessageHandler("RESET_POSITION", OnResetPosition);
        UnityMessageManager.Instance.UnregisterMessageHandler("TOGGLE_CONTROLS", OnToggleControls);
        UnityMessageManager.Instance.OnMessageReceived -= OnAnyMessageReceived;
    }

    private void OnSetCameraSpeed(string data)
    {
        if (float.TryParse(data, out float speed))
        {
            Debug.Log($"Setting camera speed to: {speed}");
            // Implement your camera speed logic here
            if (CameraController.Instance != null)
            {
                // Example: CameraController.Instance.SetSpeed(speed);
            }
        }
    }

    private void OnResetPosition(string data)
    {
        Debug.Log("Resetting camera position");
        // Implement your reset logic here
    }

    private void OnToggleControls(string data)
    {
        bool enabled = data.ToLower() == "true";
        Debug.Log($"Toggling controls: {enabled}");
        // Implement your toggle logic here
    }

    private void OnAnyMessageReceived(string messageType, string data)
    {
        Debug.Log($"Message received - Type: {messageType}, Data: {data}");
    }

    // Example: Send message when user performs an action
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            UnityMessageManager.Instance.SendMessageToFlutter("USER_ACTION", "Space key pressed");
        }
    }
}
