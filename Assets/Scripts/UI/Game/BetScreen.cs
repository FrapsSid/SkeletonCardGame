using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using CardGameModel = CardGame;
using CardGameRound = CardGame.Round;

public sealed class BetScreen : GameUIScreen
{
    public override ScreenId Id => ScreenId.BetScreen;
    public override bool IsModal => true;

    private RectTransform skeletonRoot;
    private RectTransform combinationsRoot;
    private TMP_Text priceText;
    private TMP_Text totalText;
    private TMP_Text statusText;
    private Button confirmButton;

    private readonly List<StakeAsset> selectedAssets = new List<StakeAsset>();
    private readonly Dictionary<DeclaredCombinationTier, Image> combinationImages = new Dictionary<DeclaredCombinationTier, Image>();
    private CardGameModel subscribedGame;
    private DeclaredCombinationTier selectedTier = DeclaredCombinationTier.Easy;
    private DeclaredCombinationTier minimumTier = DeclaredCombinationTier.Easy;

    protected override void Build()
    {
        GameUIFactory.Backdrop(transform, "Overlay", 0.44f);

        RectTransform panel = GameUIFactory.Panel(transform, "BetPanel", new Color(0.02f, 0.02f, 0.02f, 0.32f), false);
        GameUIFactory.Stretch(panel, 18f, 18f, 18f, 18f);
        panel.GetComponent<Image>().raycastTarget = true;
        GameUIFactory.AddGlow(panel.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanGlow, Vector2.zero);

        TMP_Text title = GameUIFactory.Text(panel, "Title", "СТАВКА", 48f, TextAlignmentOptions.Center, GameUITheme.White);
        GameUIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 58f));

        priceText = GameUIFactory.Text(panel, "Price", string.Empty, 24f, TextAlignmentOptions.Center, GameUITheme.MutedWhite, FontStyles.Bold);
        GameUIFactory.Anchor(priceText.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 42f));

        skeletonRoot = GameUIFactory.CreateRect("SkeletonStakeBody", panel);
        GameUIFactory.Anchor(skeletonRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(660f, 660f));

        combinationsRoot = GameUIFactory.Panel(panel, "Targets", new Color(0.02f, 0.02f, 0.02f, 0.58f));
        GameUIFactory.Anchor(combinationsRoot, new Vector2(0.82f, 0.52f), new Vector2(0.82f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(310f, 300f));
        VerticalLayoutGroup comboLayout = GameUIFactory.VerticalLayout(combinationsRoot.gameObject, 16f, TextAnchor.MiddleCenter);
        comboLayout.padding = new RectOffset(18, 18, 22, 22);

        CreateCombinationZone(DeclaredCombinationTier.Easy, "ЛЁГКАЯ");
        CreateCombinationZone(DeclaredCombinationTier.Medium, "СРЕДНЯЯ");
        CreateCombinationZone(DeclaredCombinationTier.Hard, "СЛОЖНАЯ");

        Button cancel = GameUIFactory.Button(panel, "CancelButton", "НАЗАД", () => UI.PopModal(), true);
        GameUIFactory.Anchor(cancel.GetComponent<RectTransform>(), new Vector2(0.16f, 0.5f), new Vector2(0.16f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 72f));

        confirmButton = GameUIFactory.Button(panel, "ConfirmButton", "ГОТОВ", Confirm, true);
        GameUIFactory.Anchor(confirmButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.5f), new Vector2(0.84f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 72f));

        totalText = GameUIFactory.Text(panel, "Total", string.Empty, 24f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Anchor(totalText.rectTransform, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 40f));

        statusText = GameUIFactory.Text(panel, "Status", string.Empty, 20f, TextAlignmentOptions.Center, GameUITheme.Red, FontStyles.Bold);
        GameUIFactory.Anchor(statusText.rectTransform, new Vector2(0.5f, 0.055f), new Vector2(0.5f, 0.055f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 34f));
    }

    protected override void OnShow()
    {
        selectedAssets.Clear();
        SubscribeGame();
        Refresh();
    }

    protected override void OnHide()
    {
        UnsubscribeGame();
        selectedAssets.Clear();
    }

    private void CreateCombinationZone(DeclaredCombinationTier tier, string label)
    {
        Button button = GameUIFactory.Button(combinationsRoot, label, label, () => SelectTier(tier), false);
        GameUIFactory.Layout(button.gameObject, 250f, 70f);
        combinationImages[tier] = button.GetComponent<Image>();
    }

    private void Refresh()
    {
        CardGameRound round = CurrentRound;
        Skeleton player = LocalPlayerForCurrentTurn;
        PlayerBetState state = round != null && player != null && round.playerStates.ContainsKey(player)
            ? round.playerStates[player]
            : null;

        minimumTier = state != null && state.declaredTarget.HasValue ? state.declaredTarget.Value : DeclaredCombinationTier.Easy;
        if (selectedTier < minimumTier)
            selectedTier = minimumTier;

        int price = round != null ? round.currentParticipationPrice : 0;
        int total = selectedAssets.Sum(asset => asset != null ? asset.stakeValue : 0);
        priceText.text = $"ЦЕНА УЧАСТИЯ: {price}";
        totalText.text = $"ВЫБРАНО: {total}";

        RefreshSkeleton(player);
        RefreshCombinations();

        confirmButton.interactable = round != null && player != null && total >= price && CanConfirm(round, player, total);
    }

    private void RefreshSkeleton(Skeleton player)
    {
        GameUIFactory.ClearChildren(skeletonRoot);

        List<StakeAsset> assets = player != null && player.team != null
            ? player.team.Assets.Where(asset => asset != null && player.team.OwnsAsset(asset)).ToList()
            : new List<StakeAsset>();

        DrawSkeletonBone("Spine", new Vector2(0f, 64f), new Vector2(22f, 240f));
        DrawSkeletonBone("Shoulders", new Vector2(0f, 130f), new Vector2(250f, 18f));
        DrawSkeletonBone("Hips", new Vector2(0f, -74f), new Vector2(180f, 18f));

        CreateSkeletonSlot("Head", BodyPartType.Head, TakeAsset(assets, BodyPartType.Head, 0), new Vector2(0f, 236f), new Vector2(122f, 122f), true);
        CreateSkeletonSlot("Soul", BodyPartType.Soul, TakeAsset(assets, BodyPartType.Soul, 1), new Vector2(0f, 90f), new Vector2(132f, 154f), false);
        CreateSkeletonSlot("LeftArm", BodyPartType.LeftArm, TakeAsset(assets, BodyPartType.LeftArm, 2), new Vector2(-165f, 64f), new Vector2(94f, 230f), false);
        CreateSkeletonSlot("RightArm", BodyPartType.RightArm, TakeAsset(assets, BodyPartType.RightArm, 3), new Vector2(165f, 64f), new Vector2(94f, 230f), false);
        CreateSkeletonSlot("LeftLeg", BodyPartType.LeftLeg, TakeAsset(assets, BodyPartType.LeftLeg, 4), new Vector2(-64f, -190f), new Vector2(96f, 250f), false);
        CreateSkeletonSlot("RightLeg", BodyPartType.RightLeg, TakeAsset(assets, BodyPartType.RightLeg, 5), new Vector2(64f, -190f), new Vector2(96f, 250f), false);
    }

    private void DrawSkeletonBone(string name, Vector2 position, Vector2 size)
    {
        RectTransform bone = GameUIFactory.CreateRect(name, skeletonRoot);
        GameUIFactory.Anchor(bone, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Image image = GameUIFactory.Image(bone, new Color(0f, 1f, 0.68f, 0.34f));
        image.raycastTarget = false;
    }

    private void CreateSkeletonSlot(string name, BodyPartType type, StakeAsset asset, Vector2 position, Vector2 size, bool circle)
    {
        Button button = GameUIFactory.Button(skeletonRoot, name, asset != null ? AssetLabel(asset) : "СТАВКА", () => ToggleAsset(asset), false);
        RectTransform rect = button.GetComponent<RectTransform>();
        GameUIFactory.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        Image image = button.GetComponent<Image>();
        if (circle)
            GameUIFactory.UseCircleSprite(image);

        button.interactable = asset != null;
        image.color = asset != null && selectedAssets.Contains(asset)
            ? GameUITheme.ButtonPressed
            : asset != null ? new Color(0f, 1f, 0.68f, 0.72f) : new Color(0.4f, 0.78f, 0.67f, 0.42f);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.fontSize = asset != null ? 18f : 14f;
            text.alignment = TextAlignmentOptions.Center;
        }

        GameTooltipTarget tooltip = button.gameObject.AddComponent<GameTooltipTarget>();
        tooltip.Set(type.ToString(), asset != null ? $"Стоимость: {asset.stakeValue}" : "Нет доступного актива");
    }

    private StakeAsset TakeAsset(List<StakeAsset> assets, BodyPartType preferredType, int fallbackIndex)
    {
        StakeAsset byPart = assets.FirstOrDefault(asset => asset.bodyPart != null && asset.bodyPart.Type == preferredType);
        if (byPart != null)
            return byPart;

        return fallbackIndex >= 0 && fallbackIndex < assets.Count ? assets[fallbackIndex] : null;
    }

    private void RefreshCombinations()
    {
        foreach (KeyValuePair<DeclaredCombinationTier, Image> pair in combinationImages)
        {
            bool disabled = pair.Key < minimumTier;
            bool selected = pair.Key == selectedTier;
            Button button = pair.Value.GetComponent<Button>();
            if (button != null)
                button.interactable = !disabled;

            pair.Value.color = disabled
                ? GameUITheme.ButtonDisabled
                : selected ? GameUITheme.ButtonPressed : GameUITheme.ButtonBase;
        }
    }

    private void ToggleAsset(StakeAsset asset)
    {
        if (asset == null)
            return;

        if (selectedAssets.Contains(asset))
            selectedAssets.Remove(asset);
        else
            selectedAssets.Add(asset);

        Refresh();
    }

    private void SelectTier(DeclaredCombinationTier tier)
    {
        if (tier < minimumTier)
            return;

        selectedTier = tier;
        Refresh();
    }

    private bool CanConfirm(CardGameRound round, Skeleton player, int total)
    {
        return total > round.currentParticipationPrice
            ? round.CanRaise(player, selectedAssets, selectedTier)
            : round.CanCall(player, selectedAssets, selectedTier);
    }

    private void Confirm()
    {
        CardGameRound round = CurrentRound;
        Skeleton player = LocalPlayerForCurrentTurn;
        if (round == null || player == null)
        {
            statusText.text = "Waiting for your turn.";
            Refresh();
            return;
        }

        if (round.CurrentPlayer != player)
        {
            statusText.text = "Waiting for your turn.";
            Refresh();
            return;
        }

        try
        {
            int total = selectedAssets.Sum(asset => asset != null ? asset.stakeValue : 0);
            if (total > round.currentParticipationPrice)
                round.Raise(player, selectedAssets, selectedTier);
            else
                round.Call(player, selectedAssets, selectedTier);

            UI.PopModal();
        }
        catch (Exception exception)
        {
            statusText.text = exception.Message;
            Refresh();
        }
    }

    private void SubscribeGame()
    {
        subscribedGame = UI.GameManager != null ? UI.GameManager.CardGame : null;
        if (subscribedGame == null)
            return;

        subscribedGame.OnPriceRaised += HandleBetChanged;
        subscribedGame.OnPriceMatched += HandleBetChanged;
        subscribedGame.OnTargetDeclared += HandleTargetChanged;
        subscribedGame.OnTargetUpgraded += HandleTargetChanged;
        subscribedGame.OnPlayerFolded += HandlePlayerChanged;
    }

    private void UnsubscribeGame()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnPriceRaised -= HandleBetChanged;
        subscribedGame.OnPriceMatched -= HandleBetChanged;
        subscribedGame.OnTargetDeclared -= HandleTargetChanged;
        subscribedGame.OnTargetUpgraded -= HandleTargetChanged;
        subscribedGame.OnPlayerFolded -= HandlePlayerChanged;
        subscribedGame = null;
    }

    private void HandleBetChanged(Skeleton player, int price) => Refresh();
    private void HandleTargetChanged(Skeleton player, DeclaredCombinationTier tier) => Refresh();
    private void HandlePlayerChanged(Skeleton player) => Refresh();

    private CardGameRound CurrentRound => UI.GameManager != null && UI.GameManager.CardGame != null ? UI.GameManager.CardGame.round : null;
    private Skeleton LocalPlayerForCurrentTurn => UI.GameManager != null && UI.GameManager.LocalPlayer == UI.GameManager.CardGame?.round?.CurrentPlayer ? UI.GameManager.LocalPlayer : null;

    private static string AssetLabel(StakeAsset asset)
    {
        if (asset == null)
            return "СТАВКА";

        string name = asset.bodyPart != null ? asset.bodyPart.Type.ToString() : asset.assetType.ToString();
        return $"{name}\n{asset.stakeValue}";
    }
}
