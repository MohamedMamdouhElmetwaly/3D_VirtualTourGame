using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Essential script for flutter_embed_unity package.
/// Handles sending string messages from Unity to the Flutter EmbedUnity widget.
/// </summary>
public static class SendToFlutter
{
    /// <summary>
    /// Sends a message string to Flutter. 
    /// In Flutter, this triggers the 'onMessageFromUnity' callback on the EmbedUnity widget.
    /// </summary>
    /// <param name="message">The string message to send.</param>
    public static void Send(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

#if UNITY_EDITOR
        Debug.Log($"[Unity->Flutter] (Editor) Message Sent: {message}");
#elif UNITY_ANDROID
        try
        {
            using (AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // flutter_embed_unity package expects this specific method on the activity
                    activity.Call("onMessageFromUnity", message);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SendToFlutter] Android error: {e.Message}");
        }
#elif UNITY_IOS
        try
        {
            _sendToFlutter(message);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SendToFlutter] iOS error: {e.Message}");
        }
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _sendToFlutter(string message);
#endif
}
