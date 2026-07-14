#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class AuthSceneSetup
{
    [MenuItem("Tools/Create Auth Scene")]
    public static void CreateAuthScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var canvasGo = new GameObject("AuthCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        canvasGo.AddComponent<AuthUINetwork>();

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        var bootstrapGo = new GameObject("AuthBootstrap");
        bootstrapGo.AddComponent<AuthBootstrap>();

        string scenePath = "Assets/Scenes/UI/Auth.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        AddSceneToBuildSettings(scenePath);

        Debug.Log($"[Auth Setup] Scene created at {scenePath} and added to Build Settings as the first scene.");
        EditorUtility.DisplayDialog("Auth Scene Setup",
            "Auth scene created!\n\n" +
            "Scene saved at: Assets/Scenes/UI/Auth.unity\n" +
            "Added to Build Settings as the first scene.\n\n" +
            "You can now open the scene and customize the UI.",
            "OK");
    }

    [MenuItem("Tools/Setup Auth Flow (Full)")]
    public static void SetupAuthFlow()
    {
        string authScenePath = "Assets/Scenes/UI/Auth.unity";

        if (!System.IO.File.Exists(authScenePath))
        {
            CreateAuthScene();
        }

        AddSceneToBuildSettings(authScenePath);

        Debug.Log("[Auth Setup] Auth flow configured. Auth scene is the first in Build Settings.");
        EditorUtility.DisplayDialog("Auth Flow Setup",
            "Auth flow is configured!\n\n" +
            "The Auth scene is now the first scene in Build Settings.\n" +
            "When the game starts, players will see the login/register screen first.",
            "OK");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (var s in buildScenes)
        {
            if (s.path == scenePath)
            {
                return;
            }
        }

        buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
    }
}
#endif
