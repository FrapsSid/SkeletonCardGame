#nullable enable

using UnityEngine;

[AddComponentMenu("UI/UI State Input Reader")]
[DisallowMultipleComponent]
public sealed class UIStateInputReader : MonoBehaviour
{
    [SerializeField] private UIStateController uiStateController = null!;

    [Header("Keys")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.Tab;
    [SerializeField] private KeyCode turnUiKey = KeyCode.Return;
    [SerializeField] private KeyCode escapeKey = KeyCode.Escape;
    [SerializeField] private KeyCode bettingKey = KeyCode.None;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (uiStateController == null)
            return;

        if (WasPressed(inventoryKey))
            uiStateController.ToggleInventory();

        if (WasPressed(turnUiKey))
            uiStateController.ToggleTurnUi();

        if (WasPressed(escapeKey))
            uiStateController.HandleEsc();

        if (WasPressed(bettingKey))
            uiStateController.OpenBetting();
    }

    private void ResolveReferences()
    {
        if (uiStateController == null)
            uiStateController = GetComponent<UIStateController>();

        if (uiStateController == null)
            uiStateController = FindFirstObjectByType<UIStateController>();
    }

    private static bool WasPressed(KeyCode keyCode)
    {
        return keyCode != KeyCode.None && InputKeyUtils.WasPressedThisFrame(keyCode);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif
}
