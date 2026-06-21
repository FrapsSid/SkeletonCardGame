#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
public sealed class TestRuntimeBootstrapper : MonoBehaviour
{
    private const string TestSceneName = "GameTest";
    private bool hudEntryRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        EnsureTest(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureTest(scene);
    }

    private static void EnsureTest(Scene scene)
    {
        if (!scene.IsValid() || scene.name != TestSceneName)
            return;

        SkeletonGameUIBootstrap.EnsureRuntimeUI();

        GameManager? manager = FindFirstObjectByType<GameManager>();
        GameObject root = manager != null
            ? manager.gameObject
            : new GameObject("Test Game");

        if (root.GetComponent<BettingDiscussionGate>() == null)
            root.AddComponent<BettingDiscussionGate>();
        if (root.GetComponent<GameManager>() == null)
            manager = root.AddComponent<GameManager>();
        else
            manager = root.GetComponent<GameManager>();
        if (root.GetComponent<TestAiTurnAdapter>() == null)
            root.AddComponent<TestAiTurnAdapter>();
        if (root.GetComponent<TestConsoleLogger>() == null)
            root.AddComponent<TestConsoleLogger>();
        if (manager != null && manager.CardGame == null && root.GetComponent<GameManagerTestBootstrapper>() == null)
            root.AddComponent<GameManagerTestBootstrapper>();
        if (root.GetComponent<TestRuntimeBootstrapper>() == null)
            root.AddComponent<TestRuntimeBootstrapper>();

        EnterTestHud();
    }

    private static void EnterTestHud()
    {
        GameUIManager? ui = FindFirstObjectByType<GameUIManager>();
        if (ui == null)
            return;

        ui.RefreshGameManager();
        ui.EnterGameHud();
    }

    private IEnumerator Start()
    {
        if (hudEntryRequested)
            yield break;

        hudEntryRequested = true;
        yield return null;

        EnterTestHud();
    }
}
