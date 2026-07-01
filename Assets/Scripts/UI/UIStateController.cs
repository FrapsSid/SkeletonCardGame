#nullable enable

using UnityEngine;

[AddComponentMenu("UI/UI State Controller")]
[DisallowMultipleComponent]
public sealed class UIStateController : MonoBehaviour
{
    private enum ModalUi
    {
        None,
        Betting,
        EscapeMenu
    }

    [Header("Base UI")]
    [SerializeField] private InventoryCanvasUI inventorySurface = null!;
    [SerializeField] private TurnActionButtons turnSurface = null!;

    [Header("Modal UI")]
    [SerializeField] private BettingCanvasUI bettingSurface = null!;
    [SerializeField] private CanvasToggle escapeMenuSurface = null!;

    private bool inventoryOpen;
    private bool turnUiOpen;
    private ModalUi openModal;

    public bool AnyUiOpen => HasVisibleUiOpen();
    public bool IsInventoryOpen => inventoryOpen;
    private void Start()
    {
        CaptureInitialBaseState();
        ApplyBaseLayer();
    }

    private void LateUpdate()
    {
        ApplyCursorState();
    }

    public void OpenInventory()
    {
        inventoryOpen = true;
        ApplyBaseLayer();
    }

    public void CloseInventory()
    {
        inventoryOpen = false;
        ApplyBaseLayer();
    }

    public void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;
        ApplyBaseLayer();
    }

    public void OpenTurnUi()
    {
        turnUiOpen = true;
        ApplyBaseLayer();
    }

    public void CloseTurnUi()
    {
        turnUiOpen = false;
        ApplyBaseLayer();
    }

    public void ToggleTurnUi()
    {
        turnUiOpen = !turnUiOpen;
        ApplyBaseLayer();
    }

    public void OpenBetting()
    {
        if (openModal == ModalUi.Betting)
            return;

        CloseModal();
        openModal = ModalUi.Betting;
        ApplyBaseLayer();
        bettingSurface.Show();
    }

    public void CloseBetting()
    {
        if (openModal != ModalUi.Betting)
            return;

        CloseModal();
        ApplyBaseLayer();
    }

    public void CloseCurrent()
    {
        if (openModal != ModalUi.None)
        {
            CloseModal();
            ApplyBaseLayer();
            return;
        }

        CloseBaseLayer();
    }

    public void HandleEsc()
    {
        if (openModal != ModalUi.None)
        {
            CloseCurrent();
            return;
        }

        if (HasAnyBaseOpen())
        {
            CloseBaseLayer();
            return;
        }

        OpenEscapeMenu();
    }

    private bool HasAnyBaseOpen()
    {
        return IsInventoryOpen || turnUiOpen;
    }

    private void OpenEscapeMenu()
    {
        if (openModal == ModalUi.EscapeMenu)
            return;

        openModal = ModalUi.EscapeMenu;
        ApplyBaseLayer();
        escapeMenuSurface.Show();
    }

    private void CloseBaseLayer()
    {
        inventoryOpen = false;
        turnUiOpen = false;
        ApplyBaseLayer();
    }

    private void CloseModal()
    {
        switch (openModal)
        {
            case ModalUi.Betting:
                bettingSurface.Hide();
                break;
            case ModalUi.EscapeMenu:
                escapeMenuSurface.Hide();
                break;
        }

        openModal = ModalUi.None;
    }

    private void ApplyBaseLayer()
    {
        bool showBase = openModal == ModalUi.None;
        ApplyInventory(showBase && inventoryOpen);
        ApplyTurnUi(showBase && turnUiOpen);
    }

    private void ApplyInventory(bool visible)
    {
        if (visible)
        {
            inventorySurface.Show();
            return;
        }

        inventorySurface.Hide();
    }

    private void ApplyTurnUi(bool visible)
    {
        if (visible)
        {
            turnSurface.Show();
            return;
        }

        turnSurface.Hide();
    }

    private void CaptureInitialBaseState()
    {
        inventoryOpen = inventorySurface.IsVisible;
        turnUiOpen = turnSurface.IsVisible;
    }

    private void ApplyCursorState()
    {
        UICursorState.Apply(AnyUiOpen);
    }

    private bool HasVisibleUiOpen()
    {
        return inventorySurface.IsVisible
            || turnSurface.IsVisible
            || bettingSurface.IsOpen
            || escapeMenuSurface.IsVisible;
    }
}
