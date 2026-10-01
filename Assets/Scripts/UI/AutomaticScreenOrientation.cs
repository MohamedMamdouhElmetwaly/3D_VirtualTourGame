using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps the complete app aligned with the physical device orientation while
/// respecting the user's operating-system rotation-lock preference.
/// </summary>
public sealed class AutomaticScreenOrientation : MonoBehaviour
{
    private const string ManagerName = "AutomaticScreenOrientation";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateManager()
    {
        if (GameObject.Find(ManagerName) != null)
            return;

        GameObject managerObject = new GameObject(ManagerName);
        DontDestroyOnLoad(managerObject);
        managerObject.AddComponent<AutomaticScreenOrientation>();
    }

    private void Awake()
    {
        ConfigureUnityAutoRotation();
    }

    private IEnumerator Start()
    {
        // Let the native mobile Activity finish attaching before applying its
        // OS-aware orientation mode. This is important when Unity is in Flutter.
        yield return null;
        yield return null;

#if UNITY_ANDROID && !UNITY_EDITOR
        RequestAndroidUserOrientation();
#endif
    }

    private static void ConfigureUnityAutoRotation()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void RequestAndroidUserOrientation()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // ActivityInfo.SCREEN_ORIENTATION_USER follows the sensor only
                // when the user has enabled auto-rotate in Android settings.
                activity?.Call("setRequestedOrientation", 2);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[AutomaticScreenOrientation] Android orientation request failed: {exception.Message}");
        }
    }
#endif
}
