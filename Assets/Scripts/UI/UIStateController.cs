#nullable enable

using System.Collections.Generic;
using Interactions;
using UnityEngine;

[AddComponentMenu("UI/UI State Controller")]
[DisallowMultipleComponent]
public sealed class UIStateController : MonoBehaviour
{
    private enum ModalUi
    {
        None,
        Betting,
        InteractionMenu,
        EscapeMenu,
        Settings
    }

    [Header("Base UI")]
    [SerializeField] private InventoryCanvasUI inventorySurface = null!;
    [SerializeField] private TurnActionButtons turnSurface = null!;

    [Header("Modal UI")]
    [SerializeField] private BettingCanvasUI bettingSurface = null!;
    [SerializeField] private InteractionMenuUI interactionMenuSurface = null!;
    [SerializeField] private CanvasToggle escapeMenuSurface = null!;
    [SerializeField] private CanvasToggle settingsSurface = null!;

    private bool inventoryOpen;
    private bool turnUiOpen;
    private ModalUi openModal;
    private bool returnToEscapeMenuAfterSettings;

    public bool AnyUiOpen => HasVisibleUiOpen();
    public bool IsInventoryOpen => inventoryOpen;

    private void OnEnable()
    {
        if (interactionMenuSurface != null)
            interactionMenuSurface.InteractionSelected += CloseInteractionMenu;

        if (turnSurface != null)
        {
            turnSurface.LocalPlayerTurnStarted += OpenTurnUi;
            turnSurface.LocalPlayerTurnEnded += CloseTurnUi;
        }
    }

    private void OnDisable()
    {
        if (interactionMenuSurface != null)
            interactionMenuSurface.InteractionSelected -= CloseInteractionMenu;

        if (turnSurface != null)
        {
            turnSurface.LocalPlayerTurnStarted -= OpenTurnUi;
            turnSurface.LocalPlayerTurnEnded -= CloseTurnUi;
        }
    }

    private void Start()
    {
        CaptureInitialBaseState();
        turnUiOpen = turnSurface.CanShowForLocalPlayer;
        ApplyBaseLayer();
    }

    private void LateUpdate()
    {
        ApplyCursorState();
    }

    public void OpenInventory()
    {
        if (IsLocalPlayerGhost()) return;
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
        if (IsLocalPlayerGhost()) return;
        if (!turnSurface.CanShowForLocalPlayer)
            return;

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
        print("TOGGLE TURN UI");
        if (turnUiOpen)
            CloseTurnUi();
        else
            OpenTurnUi();
    }

    public void OpenBetting()
    {
        if (IsLocalPlayerGhost()) return;
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

    public void OpenSettings()
    {
        if (settingsSurface == null)
        {
            Debug.LogWarning($"{nameof(UIStateController)} has no settings surface assigned.", this);
            return;
        }

        if (openModal == ModalUi.Settings)
            return;

        bool restoreEscapeMenu = openModal == ModalUi.EscapeMenu;
        CloseModal();
        returnToEscapeMenuAfterSettings = restoreEscapeMenu;
        openModal = ModalUi.Settings;
        ApplyBaseLayer();
        settingsSurface.Show();
    }

    public void CloseCurrent()
    {
        if (openModal != ModalUi.None)
        {
            bool restoreEscapeMenu = openModal == ModalUi.Settings && returnToEscapeMenuAfterSettings;
            CloseModal();

            if (restoreEscapeMenu)
            {
                OpenEscapeMenu();
                return;
            }

            ApplyBaseLayer();
            return;
        }

        CloseBaseLayer();
    }

    public void OpenInteractionMenu(IList<Interaction> interactions)
    {
        if (interactionMenuSurface == null)
        {
            Debug.LogWarning($"{nameof(UIStateController)} has no interaction menu surface assigned.", this);
            return;
        }

        if (interactions.Count == 0)
            return;

        if (openModal != ModalUi.InteractionMenu)
        {
            CloseModal();
            openModal = ModalUi.InteractionMenu;
            ApplyBaseLayer();
        }

        interactionMenuSurface.Show(interactions);
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
            case ModalUi.InteractionMenu:
                if (interactionMenuSurface != null)
                    interactionMenuSurface.Hide();
                break;
            case ModalUi.EscapeMenu:
                escapeMenuSurface.Hide();
                break;
            case ModalUi.Settings:
                if (settingsSurface != null)
                    settingsSurface.Hide();
                break;
        }

        returnToEscapeMenuAfterSettings = false;
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
            || (interactionMenuSurface != null && interactionMenuSurface.IsVisible)
            || escapeMenuSurface.IsVisible
            || (settingsSurface != null && settingsSurface.IsVisible);
    }

    private void CloseInteractionMenu()
    {
        if (openModal != ModalUi.InteractionMenu)
        {
            if (interactionMenuSurface != null)
                interactionMenuSurface.Hide();
            return;
        }

        CloseModal();
        ApplyBaseLayer();
    }

    private bool IsLocalPlayerGhost()
    {
        var gm = FindFirstObjectByType<GameManager>();
        return gm?.LocalPlayer?.IsGhost == true;
    }
}
