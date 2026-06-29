using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Canvas Key Toggle")]
[DisallowMultipleComponent]
public class CanvasKeyToggle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject targetRoot;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GraphicRaycaster targetGraphicRaycaster;
    [SerializeField] private CanvasGroup targetCanvasGroup;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("State")]
    [SerializeField] private bool openOnStart = false;
    [SerializeField] private bool addCanvasGroupWhenMissing = true;
    [SerializeField] private bool controlTargetGameObjectActive = false;

    [Header("Modal Blocking")]
    [SerializeField] private bool blockOpenWhenBettingCanvasIsOpen = true;
    [SerializeField] private bool closeOtherKeyTogglesWhenOpened = true;

    public bool IsOpen { get; private set; }

    private void Reset()
    {
        targetRoot = gameObject;
        targetCanvas = GetComponent<Canvas>();
        targetGraphicRaycaster = GetComponent<GraphicRaycaster>();
        targetCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        ResolveReferences();
        SetOpen(openOnStart);
    }

    private void Update()
    {
        if (InputKeyUtils.WasPressedThisFrame(toggleKey))
        {
            if (IsOpen)
                Close();
            else
                Open();
        }
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (!CanOpen())
        {
            return;
        }

        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool isOpen)
    {
        ResolveReferences();
        IsOpen = isOpen;

        if (isOpen && closeOtherKeyTogglesWhenOpened)
        {
            CloseOtherKeyToggles();
        }

        if (controlTargetGameObjectActive && targetRoot != null && targetRoot != gameObject)
        {
            targetRoot.SetActive(isOpen);
        }

        if (targetCanvas != null)
        {
            targetCanvas.enabled = isOpen;
        }

        if (targetGraphicRaycaster != null)
        {
            targetGraphicRaycaster.enabled = isOpen;
        }

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = isOpen ? 1f : 0f;
            targetCanvasGroup.interactable = isOpen;
            targetCanvasGroup.blocksRaycasts = isOpen;
        }
    }

    private bool CanOpen()
    {
        return !blockOpenWhenBettingCanvasIsOpen || !IsAnyBettingCanvasOpen();
    }

    private static bool IsAnyBettingCanvasOpen()
    {
        BettingCanvasUI[] bettingScreens = FindObjectsByType<BettingCanvasUI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (BettingCanvasUI bettingScreen in bettingScreens)
        {
            if (bettingScreen != null && bettingScreen.IsOpen)
            {
                return true;
            }
        }

        return false;
    }

    private void CloseOtherKeyToggles()
    {
        CanvasKeyToggle[] toggles = FindObjectsByType<CanvasKeyToggle>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (CanvasKeyToggle toggle in toggles)
        {
            if (toggle != null && toggle != this && toggle.IsOpen)
            {
                toggle.Close();
            }
        }
    }

    private void ResolveReferences()
    {
        if (targetRoot == null)
        {
            targetRoot = gameObject;
        }

        if (targetCanvas == null)
        {
            targetCanvas = targetRoot != null ? targetRoot.GetComponent<Canvas>() : GetComponent<Canvas>();
        }

        if (targetGraphicRaycaster == null)
        {
            targetGraphicRaycaster = targetRoot != null ? targetRoot.GetComponent<GraphicRaycaster>() : GetComponent<GraphicRaycaster>();
        }

        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = targetRoot != null ? targetRoot.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();
        }

        if (targetCanvasGroup == null && addCanvasGroupWhenMissing && targetRoot != null)
        {
            targetCanvasGroup = targetRoot.AddComponent<CanvasGroup>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetRoot == null)
        {
            targetRoot = gameObject;
        }

        if (targetCanvas == null)
        {
            targetCanvas = targetRoot != null ? targetRoot.GetComponent<Canvas>() : GetComponent<Canvas>();
        }

        if (targetGraphicRaycaster == null)
        {
            targetGraphicRaycaster = targetRoot != null ? targetRoot.GetComponent<GraphicRaycaster>() : GetComponent<GraphicRaycaster>();
        }

        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = targetRoot != null ? targetRoot.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();
        }
    }
#endif
}
