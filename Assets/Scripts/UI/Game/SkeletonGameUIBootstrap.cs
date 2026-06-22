using Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        EnsureRuntimeUIForScene(SceneManager.GetActiveScene());
    }

    private static void EnsureRuntimeUIForScene(Scene scene)
    {
        EnsureEventSystem();
        DisableLegacyConnectionUi();

        if (TryRefreshSceneGameUIManager(scene))
            return;

        CreateFallbackGameUIManager();
    }

    private static void CreateFallbackGameUIManager()
    {
        GameObject root = new GameObject("Skeleton Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(GameUITheme.ReferenceWidth, GameUITheme.ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GameUIManager>();
    }

    private static bool TryRefreshSceneGameUIManager(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        GameUIManager[] managers = FindObjectsByType<GameUIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameUIManager manager in managers)
        {
            if (manager == null || manager.gameObject.scene != scene)
                continue;

            manager.RefreshGameManager();
            return true;
        }

        return false;
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

    private static void DisableLegacyConnectionUi()
    {
        ConnectionUI[] legacyUis = FindObjectsByType<ConnectionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ConnectionUI legacyUi in legacyUis)
            legacyUi.gameObject.SetActive(false);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureRuntimeUIForScene(scene);
    }
}
