using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DefaultExecutionOrder(-1000)]
public sealed class SkeletonGameUIBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureRuntimeUI()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnSceneLoaded(scene);
    }

    private static void OnSceneLoaded(Scene scene)
    {
        EnsureEventSystem();
        RefreshUIManagers(scene);
    }

    private static void RefreshUIManagers(Scene scene)
    {
        if (!scene.IsValid())
            return;
        GameUIManager[] managers =
            FindObjectsByType<GameUIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameUIManager manager in managers)
        {
            if (manager == null || manager.gameObject.scene != scene)
                continue;
            manager.RefreshGameManager();
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        DontDestroyOnLoad(eventSystem);
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }
}