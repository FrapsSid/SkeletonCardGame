using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ScreenId
{
    None,
    MainMenu,
    Settings,
    QuickGameLobby,
    CustomGameLobby,
    JoinGame,
    PauseMenu,
    TurnActionMenu,
    Inventory,
    BetScreen
}

public sealed class InventoryScreen : GameUIScreen
{
    private const int DefaultSlotCount = 9;

    private readonly List<InventoryDisplaySlot> slotViews = new List<InventoryDisplaySlot>();
    private RectTransform gridRoot;
    private Inventory inventory;

    public override ScreenId Id => ScreenId.Inventory;

    protected override void Build()
    {
        RectTransform overlay = GameUIFactory.CreateRect("Overlay", transform);
        GameUIFactory.Stretch(overlay);
        GameUIFactory.Image(overlay, new Color(0f, 0f, 0f, 0.62f));

        RectTransform frame = GameUIFactory.Panel(transform, "InventoryFrame", new Color(0.02f, 0.02f, 0.02f, 0.46f), false);
        GameUIFactory.Stretch(frame, 16f, 16f, 16f, 16f);
        frame.GetComponent<Image>().raycastTarget = false;
        GameUIFactory.AddGlow(frame.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanGlow, Vector2.zero);

        TMP_Text title = GameUIFactory.Text(transform, "Title", "ИНВЕНТАРЬ", 38f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 56f));

        gridRoot = GameUIFactory.CreateRect("Grid", transform);
        GameUIFactory.Anchor(gridRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));

        GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(142f, 142f);
        grid.spacing = new Vector2(24f, 24f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        EnsureSlotViews(DefaultSlotCount);

        Button back = GameUIFactory.Button(transform, "BackButton", "НАЗАД", () => UI.CloseScreen(ScreenId.Inventory), true);
        GameUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.09f), new Vector2(0.5f, 0.09f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 64f));
    }

    protected override void OnShow()
    {
        SubscribeToInventory(FindPlayerInventory());
        RefreshSlots();
    }

    protected override void OnHide()
    {
        SubscribeToInventory(null);
    }

    private void OnDestroy()
    {
        SubscribeToInventory(null);
    }

    private void SubscribeToInventory(Inventory target)
    {
        if (inventory == target)
            return;

        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshSlots;

        inventory = target;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshSlots;
    }

    private void RefreshSlots()
    {
        EnsureSlotViews(DefaultSlotCount);

        for (int i = 0; i < slotViews.Count; i++)
        {
            InventorySlot slot = inventory != null && inventory.slots != null && i < inventory.slots.Count
                ? inventory.slots[i]
                : null;
            slotViews[i].SetSlot(slot);
        }
    }

    private void EnsureSlotViews(int count)
    {
        if (gridRoot == null)
            return;

        count = Mathf.Max(0, count);

        while (slotViews.Count < count)
            slotViews.Add(CreateSlot(gridRoot, slotViews.Count));

        while (slotViews.Count > count)
        {
            int lastIndex = slotViews.Count - 1;
            InventoryDisplaySlot slot = slotViews[lastIndex];
            slotViews.RemoveAt(lastIndex);

            if (slot.Root != null)
                Destroy(slot.Root.gameObject);
        }
    }

    private static Inventory FindPlayerInventory()
    {
        PlayerInventoryOwner owner = FindFirstObjectByType<PlayerInventoryOwner>();
        if (owner != null)
        {
            if (owner.Inventory != null)
                return owner.Inventory;

            Inventory ownerInventory = owner.GetComponent<Inventory>();
            if (ownerInventory != null)
                return ownerInventory;
        }

        return FindFirstObjectByType<Inventory>();
    }

    private static InventoryDisplaySlot CreateSlot(Transform parent, int index)
    {
        RectTransform slot = GameUIFactory.CreateRect("Slot " + index, parent);
        Image image = GameUIFactory.Image(slot, new Color(0.14f, 0.14f, 0.14f, 0.82f));
        GameUIFactory.UseRoundedSprite(image);
        GameUIFactory.AddGlow(slot.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanGlow, new Vector2(2f, -2f));

        RectTransform iconRect = GameUIFactory.CreateRect("Icon", slot);
        GameUIFactory.Anchor(iconRect, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
        Image icon = GameUIFactory.Image(iconRect, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text name = GameUIFactory.Text(slot, "Name", string.Empty, 15f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Anchor(name.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(106f, 32f));
        name.textWrappingMode = TextWrappingModes.Normal;
        name.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text count = GameUIFactory.Text(slot, "Count", string.Empty, 18f, TextAlignmentOptions.Right, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Anchor(count.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(52f, 28f));

        return new InventoryDisplaySlot(slot, image, icon, name, count);
    }

    private sealed class InventoryDisplaySlot
    {
        private readonly Image background;
        private readonly Image icon;
        private readonly TMP_Text name;
        private readonly TMP_Text count;

        public InventoryDisplaySlot(RectTransform root, Image background, Image icon, TMP_Text name, TMP_Text count)
        {
            Root = root;
            this.background = background;
            this.icon = icon;
            this.name = name;
            this.count = count;
        }

        public RectTransform Root { get; }

        public void SetSlot(InventorySlot slot)
        {
            bool hasItem = slot != null && !slot.IsEmpty && slot.quantity > 0;
            ItemData item = hasItem ? slot.itemData : null;
            bool hasIcon = item != null && item.icon != null;

            if (background != null)
                background.color = hasItem ? new Color(0.16f, 0.2f, 0.2f, 0.9f) : new Color(0.14f, 0.14f, 0.14f, 0.82f);

            if (icon != null)
            {
                icon.enabled = hasItem;
                icon.sprite = hasIcon ? item.icon : null;
                icon.color = hasIcon ? Color.white : new Color(1f, 1f, 1f, 0.18f);
            }

            if (name != null)
                name.text = hasItem && item != null ? item.itemName : string.Empty;

            if (count != null)
                count.text = hasItem && slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
        }
    }
}
