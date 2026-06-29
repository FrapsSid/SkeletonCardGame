using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Quit Game Button")]
[DisallowMultipleComponent]
public class QuitGameButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private bool registerButtonClick = true;
#if UNITY_EDITOR
    [SerializeField] private bool stopPlayModeInEditor = true;
    [SerializeField] private bool logQuitRequestInEditor = true;
#endif

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

        button.onClick.RemoveListener(QuitGame);
        button.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(QuitGame);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        if (stopPlayModeInEditor && UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }

        if (logQuitRequestInEditor)
        {
            Debug.Log($"{nameof(QuitGameButton)} received a quit request.", this);
        }
#else
        Application.Quit();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
#endif
}
