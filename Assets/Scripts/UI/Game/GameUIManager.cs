using System;
using System.Collections.Generic;
using Multiplayer;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameUIManager : MonoBehaviour
{
    private readonly Dictionary<ScreenId, GameUIScreen> screens = new Dictionary<ScreenId, GameUIScreen>();
    private readonly Stack<ScreenId> modalStack = new Stack<ScreenId>();
    private bool initialized;
    private ScreenId activeScreen = ScreenId.None;
    private ScreenId settingsReturnScreen = ScreenId.MainMenu;
    private RectTransform showcasePanel;
    private bool uiOwnsCursor;

    [SerializeField]
    private bool openMainMenuOnStart = true;

    public event Action<ScreenId> OnScreenOpened;
    public event Action<ScreenId> OnScreenClosed;

    public GameManager GameManager { get; private set; }
    public NetworkGameManager NetworkGameManager => NetworkGameManager.Instance;
    public ScreenId ActiveScreen => activeScreen;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        TurnActionMenu turnActionMenu = GetScreen<TurnActionMenu>(ScreenId.TurnActionMenu);
        turnActionMenu?.TickAlways();

        if (InputKeyUtils.WasPressedThisFrame(KeyCode.F10))
            ToggleShowcase();

        if (!InputKeyUtils.WasPressedThisFrame(KeyCode.Escape))
        {
            ApplyCursorState();
            return;
        }

        if (modalStack.Count > 0)
        {
            PopModal();
            ApplyCursorState();
            return;
        }

        if (turnActionMenu != null && turnActionMenu.TryHandleEscape())
        {
            ApplyCursorState();
            return;
        }

        if (ShouldHandlePauseInput())
            PushModal(ScreenId.PauseMenu);

        ApplyCursorState();
    }

    public void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        RefreshGameManager();
        CreateScreens();
        CreateShowcaseControls();
        GameTooltip.Create(transform);
        if (openMainMenuOnStart)
            OpenScreen(ScreenId.MainMenu);
    }

    public void RefreshGameManager()
    {
        GameManager = FindFirstObjectByType<GameManager>();
    }

    public void OpenScreen(ScreenId id)
    {
        if (id == ScreenId.None)
        {
            CloseActiveScreen();
            CloseAllModals();
            return;
        }

        if (!screens.TryGetValue(id, out GameUIScreen screen))
            return;

        StopHudPreview();
        HideHud(ScreenId.TurnActionMenu);
        CloseAllModals();
        if (activeScreen != ScreenId.None && activeScreen != id)
            CloseScreen(activeScreen);

        activeScreen = id;
        screen.Show();
        OnScreenOpened?.Invoke(id);
        ApplyCursorState();
    }

    public void CloseScreen(ScreenId id)
    {
        if (!screens.TryGetValue(id, out GameUIScreen screen) || !screen.gameObject.activeSelf)
            return;

        screen.Hide();
        if (activeScreen == id)
            activeScreen = ScreenId.None;
        OnScreenClosed?.Invoke(id);
        ApplyCursorState();
    }

    public void CloseActiveScreen()
    {
        if (activeScreen != ScreenId.None)
            CloseScreen(activeScreen);
    }

    public void PushModal(ScreenId id)
    {
        if (!screens.TryGetValue(id, out GameUIScreen screen))
            return;

        if (id != ScreenId.BetScreen)
            StopHudPreview();
        modalStack.Push(id);
        screen.Show();
        OnScreenOpened?.Invoke(id);
        ApplyCursorState();
    }

    public void PopModal()
    {
        if (modalStack.Count == 0)
            return;

        ScreenId id = modalStack.Pop();
        if (screens.TryGetValue(id, out GameUIScreen screen))
            screen.Hide();
        OnScreenClosed?.Invoke(id);
        ApplyCursorState();
    }

    public void ShowHud(ScreenId id)
    {
        if (!screens.TryGetValue(id, out GameUIScreen screen) || screen.gameObject.activeSelf)
            return;

        screen.Show();
        OnScreenOpened?.Invoke(id);
        ApplyCursorState();
    }

    public void HideHud(ScreenId id)
    {
        if (!screens.TryGetValue(id, out GameUIScreen screen) || !screen.gameObject.activeSelf)
            return;

        screen.Hide();
        OnScreenClosed?.Invoke(id);
        ApplyCursorState();
    }

    public T GetScreen<T>(ScreenId id) where T : GameUIScreen
    {
        return screens.TryGetValue(id, out GameUIScreen screen) ? screen as T : null;
    }

    public void OpenSettings(bool modal)
    {
        if (modal)
        {
            PushModal(ScreenId.Settings);
            return;
        }

        settingsReturnScreen = activeScreen == ScreenId.None ? ScreenId.MainMenu : activeScreen;
        OpenScreen(ScreenId.Settings);
    }

    public void CloseSettings()
    {
        if (modalStack.Count > 0 && modalStack.Peek() == ScreenId.Settings)
        {
            PopModal();
            return;
        }

        OpenScreen(settingsReturnScreen == ScreenId.None ? ScreenId.MainMenu : settingsReturnScreen);
    }

    public void StartQuickGameLobby()
    {
        TryHostGame();
        QuickGameLobbyScreen lobby = GetScreen<QuickGameLobbyScreen>(ScreenId.QuickGameLobby);
        lobby?.SetRole(true);
        OpenScreen(ScreenId.QuickGameLobby);
    }

    public void StartCustomGameLobby()
    {
        TryHostGame();
        CustomGameLobbyScreen lobby = GetScreen<CustomGameLobbyScreen>(ScreenId.CustomGameLobby);
        lobby?.SetRole(true);
        OpenScreen(ScreenId.CustomGameLobby);
    }

    public void JoinLobbyAsGuest(bool customLobby)
    {
        if (customLobby)
        {
            CustomGameLobbyScreen lobby = GetScreen<CustomGameLobbyScreen>(ScreenId.CustomGameLobby);
            lobby?.SetRole(false);
            OpenScreen(ScreenId.CustomGameLobby);
        }
        else
        {
            QuickGameLobbyScreen lobby = GetScreen<QuickGameLobbyScreen>(ScreenId.QuickGameLobby);
            lobby?.SetRole(false);
            OpenScreen(ScreenId.QuickGameLobby);
        }
    }

    public void EnterGameHud()
    {
        RefreshGameManager();
        CloseActiveScreen();
        CloseAllModals();
        StopHudPreview();
    }

    public void ReturnToMainMenu()
    {
        OpenScreen(ScreenId.MainMenu);
        TryDisconnectNetwork();
    }

    public void ExitApplication()
    {
        Application.Quit();
#if UNITY_EDITOR
        Debug.Log("Exit requested from main menu.", this);
#endif
    }

    private void TryHostGame()
    {
        if (NetworkGameManager == null)
            return;

        try
        {
            NetworkGameManager.HostGame();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Unable to start host: {exception.Message}", this);
        }
    }

    private void TryDisconnectNetwork()
    {
        if (NetworkGameManager == null || NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsListening
            && !NetworkManager.Singleton.IsClient
            && !NetworkManager.Singleton.IsServer
            && !NetworkManager.Singleton.IsHost)
            return;

        try
        {
            NetworkGameManager.Disconnect();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Unable to disconnect cleanly: {exception.Message}", this);
        }
    }

    private void CloseAllModals()
    {
        while (modalStack.Count > 0)
            PopModal();
    }

    private bool ShouldHandlePauseInput()
    {
        return activeScreen == ScreenId.None;
    }

    private void ApplyCursorState()
    {
        bool shouldOwnCursor = AnyManagedScreenVisible() || IsShowcaseOpen();
        if (shouldOwnCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            uiOwnsCursor = true;
        }
        else if (uiOwnsCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            uiOwnsCursor = false;
        }
    }

    private bool AnyManagedScreenVisible()
    {
        foreach (GameUIScreen screen in screens.Values)
        {
            if (screen != null && screen.gameObject.activeSelf)
                return true;
        }

        return false;
    }

    private bool IsShowcaseOpen()
    {
        return showcasePanel != null && showcasePanel.gameObject.activeSelf;
    }

    private void ToggleShowcase()
    {
        if (showcasePanel == null)
            return;

        showcasePanel.gameObject.SetActive(!showcasePanel.gameObject.activeSelf);
        showcasePanel.SetAsLastSibling();
    }

    private void HideShowcase()
    {
        if (showcasePanel != null)
            showcasePanel.gameObject.SetActive(false);
    }

    private void CreateShowcaseControls()
    {
        RectTransform toggleRoot = GameUIFactory.CreateRect("UiShowcaseToggle", transform);
        GameUIFactory.Anchor(toggleRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(64f, 44f));

        Button toggle = GameUIFactory.Button(toggleRoot, "Toggle", "UI", ToggleShowcase, false);
        GameUIFactory.Stretch(toggle.GetComponent<RectTransform>());
        TMP_Text toggleText = toggle.GetComponentInChildren<TMP_Text>();
        if (toggleText != null)
            toggleText.fontSize = 18f;

        showcasePanel = GameUIFactory.Panel(transform, "UiShowcasePanel", new Color(0.015f, 0.018f, 0.017f, 0.94f));
        GameUIFactory.Anchor(showcasePanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -72f), new Vector2(250f, 420f));
        VerticalLayoutGroup layout = GameUIFactory.VerticalLayout(showcasePanel.gameObject, 8f, TextAnchor.UpperCenter);
        layout.padding = new RectOffset(14, 14, 14, 14);

        AddShowcaseButton("MAIN", () => PreviewScreen(ScreenId.MainMenu));
        AddShowcaseButton("SETTINGS", () => PreviewScreen(ScreenId.Settings));
        AddShowcaseButton("QUICK LOBBY", PreviewQuickLobby);
        AddShowcaseButton("CUSTOM LOBBY", PreviewCustomLobby);
        AddShowcaseButton("JOIN", () => PreviewScreen(ScreenId.JoinGame));
        AddShowcaseButton("PAUSE", () => PreviewModal(ScreenId.PauseMenu));
        AddShowcaseButton("TURN HUD", PreviewTurnHud);
        AddShowcaseButton("INVENTORY", () => PreviewScreen(ScreenId.Inventory));
        AddShowcaseButton("BET", () => PreviewModal(ScreenId.BetScreen));
        AddShowcaseButton("CLOSE", HideShowcase);

        showcasePanel.gameObject.SetActive(false);
    }

    private void AddShowcaseButton(string label, UnityEngine.Events.UnityAction action)
    {
        Button button = GameUIFactory.Button(showcasePanel, label, label, action, false);
        GameUIFactory.Layout(button.gameObject, 210f, 34f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.fontSize = 16f;
    }

    private void PreviewScreen(ScreenId id)
    {
        HideShowcase();
        OpenScreen(id);
    }

    private void PreviewModal(ScreenId id)
    {
        HideShowcase();
        StopHudPreview();
        CloseAllModals();
        PushModal(id);
    }

    private void PreviewQuickLobby()
    {
        HideShowcase();
        QuickGameLobbyScreen lobby = GetScreen<QuickGameLobbyScreen>(ScreenId.QuickGameLobby);
        lobby?.SetRole(true);
        OpenScreen(ScreenId.QuickGameLobby);
    }

    private void PreviewCustomLobby()
    {
        HideShowcase();
        CustomGameLobbyScreen lobby = GetScreen<CustomGameLobbyScreen>(ScreenId.CustomGameLobby);
        lobby?.SetRole(true);
        OpenScreen(ScreenId.CustomGameLobby);
    }

    private void PreviewTurnHud()
    {
        HideShowcase();
        CloseAllModals();
        CloseActiveScreen();

        TurnActionMenu turnActionMenu = GetScreen<TurnActionMenu>(ScreenId.TurnActionMenu);
        if (turnActionMenu != null)
            turnActionMenu.SetPreviewMode(true);
        ShowHud(ScreenId.TurnActionMenu);
    }

    private void StopHudPreview()
    {
        TurnActionMenu turnActionMenu = GetScreen<TurnActionMenu>(ScreenId.TurnActionMenu);
        if (turnActionMenu != null)
            turnActionMenu.SetPreviewMode(false);
    }

    private void CreateScreens()
    {
        RectTransform screenRoot = GameUIFactory.CreateRect("Screens", transform);
        GameUIFactory.Stretch(screenRoot);

        Register(CreateScreen<MainMenuScreen>(screenRoot, "MainMenuScreen"));
        Register(CreateScreen<SettingsScreen>(screenRoot, "SettingsScreen"));
        Register(CreateScreen<QuickGameLobbyScreen>(screenRoot, "QuickGameLobbyScreen"));
        Register(CreateScreen<CustomGameLobbyScreen>(screenRoot, "CustomGameLobbyScreen"));
        Register(CreateScreen<JoinGameScreen>(screenRoot, "JoinGameScreen"));
        Register(CreateScreen<PauseMenuScreen>(screenRoot, "PauseMenuScreen"));
        Register(CreateScreen<TurnActionMenu>(screenRoot, "TurnActionMenu"));
        Register(CreateScreen<InventoryScreen>(screenRoot, "InventoryScreen"));
        Register(CreateScreen<BetScreen>(screenRoot, "BetScreen"));
    }

    private T CreateScreen<T>(Transform parent, string name) where T : GameUIScreen
    {
        RectTransform rect = GameUIFactory.CreateRect(name, parent);
        GameUIFactory.Stretch(rect);
        T screen = rect.gameObject.AddComponent<T>();
        screen.Initialize(this);
        return screen;
    }

    private void Register(GameUIScreen screen)
    {
        screens[screen.Id] = screen;
    }
}
