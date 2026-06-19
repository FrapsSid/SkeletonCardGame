#nullable enable

using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TurnUI : MonoBehaviour
{
    [SerializeField]
    private GameManager? gameManager;

    [SerializeField]
    private UIDocument? uiDocument;

    [SerializeField]
    private bool useImmediateGuiFallback = true;

    [SerializeField]
    private Rect immediateGuiRect = new Rect(16f, 16f, 360f, 360f);

    [Header("Element Names")]
    [SerializeField]
    private string combinationDropdownName = "combination-dropdown";

    [SerializeField]
    private string raiseStakeInputName = "raise-stake-input";

    [SerializeField]
    private string callButtonName = "call-button";

    [SerializeField]
    private string raiseButtonName = "raise-button";

    [SerializeField]
    private string allInButtonName = "all-in-button";

    [SerializeField]
    private string takeCardButtonName = "take-card-button";

    [SerializeField]
    private string foldButtonName = "fold-button";

    [SerializeField]
    private string endTurnButtonName = "end-turn-button";

    [SerializeField]
    private string outputTextName = "output-text";

    private DropdownField? combinationDropdown;
    private IntegerField? raiseStakeInput;
    private Button? callButton;
    private Button? raiseButton;
    private Button? allInButton;
    private Button? takeCardButton;
    private Button? foldButton;
    private Button? endTurnButton;
    private TextElement? outputText;
    private Skeleton? shownPlayer;
    private int immediateTierIndex;
    private string immediateRaiseStake = string.Empty;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GetComponentInParent<GameManager>();
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        ResolveElements();
        AddListeners();
        Refresh();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void Show(GameManager manager, Skeleton player)
    {
        gameManager = manager;
        shownPlayer = player;
        ResolveElements();
        Refresh();
    }

    public void Refresh()
    {
        Skeleton? player = shownPlayer ?? gameManager?.ActiveTurnPlayer;
        if (outputText != null)
            outputText.text = gameManager != null ? gameManager.BuildTurnSummary(player) : "Game manager is not assigned.";

        SetButtonState(callButton, CardGameTurnAction.Call, player);
        SetButtonState(raiseButton, CardGameTurnAction.Raise, player);
        SetButtonState(allInButton, CardGameTurnAction.AllIn, player);
        SetButtonState(takeCardButton, CardGameTurnAction.TakeCard, player);
        SetButtonState(foldButton, CardGameTurnAction.Fold, player);
        SetButtonState(endTurnButton, CardGameTurnAction.EndTurn, player);

        if (raiseStakeInput != null && raiseStakeInput.value <= 0 && gameManager != null)
        {
            int suggestedStake = gameManager.GetSuggestedRaiseStake(player);
            if (suggestedStake > 0)
                raiseStakeInput.value = suggestedStake;
        }
    }

    private void OnGUI()
    {
        if (!useImmediateGuiFallback)
            return;

        immediateGuiRect = GUILayout.Window(GetInstanceID(), immediateGuiRect, DrawImmediateGui, "Card Turn");
    }

    private void ResolveElements()
    {
        VisualElement? root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
            return;

        combinationDropdown = root.Q<DropdownField>(combinationDropdownName);
        raiseStakeInput = root.Q<IntegerField>(raiseStakeInputName);
        callButton = root.Q<Button>(callButtonName);
        raiseButton = root.Q<Button>(raiseButtonName);
        allInButton = root.Q<Button>(allInButtonName);
        takeCardButton = root.Q<Button>(takeCardButtonName);
        foldButton = root.Q<Button>(foldButtonName);
        endTurnButton = root.Q<Button>(endTurnButtonName);
        outputText = root.Q<TextElement>(outputTextName);

        if (combinationDropdown != null && combinationDropdown.choices.Count == 0)
        {
            combinationDropdown.choices = new System.Collections.Generic.List<string>
            {
                DeclaredCombinationTier.Easy.ToString(),
                DeclaredCombinationTier.Medium.ToString(),
                DeclaredCombinationTier.Hard.ToString()
            };
            combinationDropdown.value = DeclaredCombinationTier.Easy.ToString();
        }
    }

    private void AddListeners()
    {
        if (callButton != null)
            callButton.clicked += HandleCallClicked;
        if (raiseButton != null)
            raiseButton.clicked += HandleRaiseClicked;
        if (allInButton != null)
            allInButton.clicked += HandleAllInClicked;
        if (takeCardButton != null)
            takeCardButton.clicked += HandleTakeCardClicked;
        if (foldButton != null)
            foldButton.clicked += HandleFoldClicked;
        if (endTurnButton != null)
            endTurnButton.clicked += HandleEndTurnClicked;
    }

    private void RemoveListeners()
    {
        if (callButton != null)
            callButton.clicked -= HandleCallClicked;
        if (raiseButton != null)
            raiseButton.clicked -= HandleRaiseClicked;
        if (allInButton != null)
            allInButton.clicked -= HandleAllInClicked;
        if (takeCardButton != null)
            takeCardButton.clicked -= HandleTakeCardClicked;
        if (foldButton != null)
            foldButton.clicked -= HandleFoldClicked;
        if (endTurnButton != null)
            endTurnButton.clicked -= HandleEndTurnClicked;
    }

    private void HandleCallClicked()
    {
        gameManager?.TryCallCurrentPlayer(GetSelectedTier());
        Refresh();
    }

    private void HandleRaiseClicked()
    {
        gameManager?.TryRaiseCurrentPlayer(GetSelectedTier(), GetRaiseStake());
        Refresh();
    }

    private void HandleAllInClicked()
    {
        gameManager?.TryAllInCurrentPlayer(GetSelectedTier());
        Refresh();
    }

    private void HandleTakeCardClicked()
    {
        gameManager?.TryTakeCardCurrentPlayer();
        Refresh();
    }

    private void HandleFoldClicked()
    {
        gameManager?.TryFoldCurrentPlayer();
        Refresh();
    }

    private void HandleEndTurnClicked()
    {
        gameManager?.TryEndCurrentTurn();
        Refresh();
    }

    private DeclaredCombinationTier GetSelectedTier()
    {
        string value = combinationDropdown != null ? combinationDropdown.value : DeclaredCombinationTier.Easy.ToString();
        return Enum.TryParse(value, out DeclaredCombinationTier tier) ? tier : DeclaredCombinationTier.Easy;
    }

    private int GetRaiseStake()
    {
        if (raiseStakeInput != null)
            return Math.Max(0, raiseStakeInput.value);

        return int.TryParse(immediateRaiseStake, out int stake) ? Math.Max(0, stake) : 0;
    }

    private void SetButtonState(Button? button, CardGameTurnAction action, Skeleton? player)
    {
        if (button == null)
            return;

        button.SetEnabled(gameManager != null && gameManager.GetValidTurnActions(player).Contains(action));
    }

    private void DrawImmediateGui(int windowId)
    {
        Skeleton? player = shownPlayer ?? gameManager?.ActiveTurnPlayer;
        GUILayout.Label(gameManager != null ? gameManager.BuildTurnSummary(player) : "Game manager is not assigned.");

        string[] tiers =
        {
            DeclaredCombinationTier.Easy.ToString(),
            DeclaredCombinationTier.Medium.ToString(),
            DeclaredCombinationTier.Hard.ToString()
        };

        immediateTierIndex = GUILayout.SelectionGrid(immediateTierIndex, tiers, tiers.Length);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Raise stake", GUILayout.Width(90f));
        immediateRaiseStake = GUILayout.TextField(immediateRaiseStake);
        GUILayout.EndHorizontal();

        DrawImmediateActionButton("Call", CardGameTurnAction.Call, () => gameManager?.TryCallCurrentPlayer(GetImmediateTier()));
        DrawImmediateActionButton("Raise", CardGameTurnAction.Raise, () => gameManager?.TryRaiseCurrentPlayer(GetImmediateTier(), GetRaiseStake()));
        DrawImmediateActionButton("All In", CardGameTurnAction.AllIn, () => gameManager?.TryAllInCurrentPlayer(GetImmediateTier()));
        DrawImmediateActionButton("Take Card", CardGameTurnAction.TakeCard, () => gameManager?.TryTakeCardCurrentPlayer());
        DrawImmediateActionButton("Fold", CardGameTurnAction.Fold, () => gameManager?.TryFoldCurrentPlayer());
        DrawImmediateActionButton("End Turn", CardGameTurnAction.EndTurn, () => gameManager?.TryEndCurrentTurn());

        GUI.DragWindow();
    }

    private void DrawImmediateActionButton(string label, CardGameTurnAction action, Func<bool?> command)
    {
        Skeleton? player = shownPlayer ?? gameManager?.ActiveTurnPlayer;
        bool isEnabled = gameManager != null && gameManager.GetValidTurnActions(player).Contains(action);
        bool wasEnabled = GUI.enabled;
        GUI.enabled = isEnabled;

        if (GUILayout.Button(label))
        {
            command.Invoke();
            Refresh();
        }

        GUI.enabled = wasEnabled;
    }

    private DeclaredCombinationTier GetImmediateTier()
    {
        return (DeclaredCombinationTier)Mathf.Clamp(immediateTierIndex + 1, (int)DeclaredCombinationTier.Easy, (int)DeclaredCombinationTier.Hard);
    }
}
