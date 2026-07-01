#nullable enable

using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Canvas Key Toggle")]
[DisallowMultipleComponent]
public class CanvasToggle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject? targetRoot;
    [SerializeField] private Canvas? targetCanvas;
    [SerializeField] private GraphicRaycaster? targetGraphicRaycaster;
    [SerializeField] private CanvasGroup? targetCanvasGroup;

    [Header("State")]
    [SerializeField] private bool openOnStart;
    [SerializeField] private bool addCanvasGroupWhenMissing = true;
    [SerializeField] private bool controlTargetGameObjectActive;

    public bool IsOpen { get; private set; }
    public bool IsVisible
    {
        get
        {
            bool canvasVisible = targetCanvas == null || targetCanvas.enabled;
            bool groupVisible = targetCanvasGroup == null || targetCanvasGroup.alpha > 0.001f;
            return isActiveAndEnabled && canvasVisible && groupVisible;
        }
    }

    private void Reset()
    {
        targetRoot = gameObject;
        targetCanvas = GetComponent<Canvas>();
        targetGraphicRaycaster = GetComponent<GraphicRaycaster>();
        targetCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        ResolveTargetReferences(true);
        SetPresentationOpen(openOnStart);
    }

    public void Show()
    {
        SetPresentationOpen(true);
    }

    public void Hide()
    {
        SetPresentationOpen(false);
    }

    private void SetPresentationOpen(bool isOpen)
    {
        ResolveTargetReferences(true);
        IsOpen = isOpen;

        if (controlTargetGameObjectActive && targetRoot != null && targetRoot != gameObject)
            targetRoot.SetActive(isOpen);

        if (targetCanvas != null)
            targetCanvas.enabled = isOpen;

        if (targetGraphicRaycaster != null)
            targetGraphicRaycaster.enabled = isOpen;

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = isOpen ? 1f : 0f;
            targetCanvasGroup.interactable = isOpen;
            targetCanvasGroup.blocksRaycasts = isOpen;
        }
    }

    private void ResolveTargetReferences(bool addMissingCanvasGroup)
    {
        if (targetRoot == null)
            targetRoot = gameObject;

        if (targetCanvas == null)
            targetCanvas = targetRoot != null ? targetRoot.GetComponent<Canvas>() : GetComponent<Canvas>();

        if (targetGraphicRaycaster == null)
            targetGraphicRaycaster = targetRoot != null
                ? targetRoot.GetComponent<GraphicRaycaster>()
                : GetComponent<GraphicRaycaster>();

        if (targetCanvasGroup == null)
            targetCanvasGroup = targetRoot != null ? targetRoot.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();

        if (targetCanvasGroup == null && addMissingCanvasGroup && addCanvasGroupWhenMissing && targetRoot != null)
            targetCanvasGroup = targetRoot.AddComponent<CanvasGroup>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveTargetReferences(false);
    }
#endif
}
