using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Toggle Object Visibility Button")]
[DisallowMultipleComponent]
public class ToggleObjectVisibilityButton : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject targetObject = null;

    [Header("Button")]
    [SerializeField] private Button button = null;
    [SerializeField] private bool registerButtonClick = true;

    [Header("Initial State")]
    [SerializeField] private bool applyInitialVisibility = false;
    [SerializeField] private bool visibleOnStart = true;

    public bool IsVisible => targetObject != null && targetObject.activeSelf;

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

        if (applyInitialVisibility)
        {
            SetVisible(visibleOnStart);
        }
    }

    private void OnEnable()
    {
        if (!registerButtonClick || button == null)
        {
            return;
        }

        button.onClick.RemoveListener(ToggleVisibility);
        button.onClick.AddListener(ToggleVisibility);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleVisibility);
        }
    }

    public void ToggleVisibility()
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"{nameof(ToggleObjectVisibilityButton)} on {name} has no target object.", this);
            return;
        }

        SetVisible(!targetObject.activeSelf);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (targetObject == null)
        {
            return;
        }

        targetObject.SetActive(visible);
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
