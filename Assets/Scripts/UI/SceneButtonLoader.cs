using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[AddComponentMenu("UI/Scene Button Loader")]
[DisallowMultipleComponent]
public class SceneButtonLoader : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset targetScene = null;
#endif
    [SerializeField] private string scenePath = string.Empty;
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;
    [SerializeField] private Button button;
    [SerializeField] private bool registerButtonClick = true;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (!registerButtonClick || button == null)
        {
            return;
        }

        button.onClick.RemoveListener(LoadTargetScene);
        button.onClick.AddListener(LoadTargetScene);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(LoadTargetScene);
        }
    }

    public void LoadTargetScene()
    {
        string target = string.IsNullOrWhiteSpace(scenePath) ? string.Empty : scenePath.Trim();
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning($"{nameof(SceneButtonLoader)} on {name} has no target scene.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError($"{nameof(SceneButtonLoader)} could not load '{target}'. Add the scene to Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(target, loadMode);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (targetScene == null)
        {
            return;
        }

        string path = UnityEditor.AssetDatabase.GetAssetPath(targetScene);
        if (!string.IsNullOrEmpty(path))
        {
            scenePath = path;
        }
    }
#endif
}
