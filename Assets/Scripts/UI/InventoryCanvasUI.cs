using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Inventory Canvas UI")]
[DisallowMultipleComponent]
public class InventoryCanvasUI : MonoBehaviour
{
    private const string RuntimeIconName = "Runtime Item Icon";
    private const string RuntimeCountName = "Runtime Item Count";
    private const string RuntimeDragIconName = "Inventory Drag Icon";

    [Header("Inventory")]
    [SerializeField] private PlayerInventoryOwner inventoryOwner;
    [SerializeField] private Inventory inventory;
    [SerializeField] private bool autoBindRuntimeInventory = true;

    [Header("Canvas")]
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform[] slotRoots = new RectTransform[0];

    [Header("Drag")]
    [SerializeField] private bool dropEntireStackWhenDraggedOutside = false;
    [SerializeField, Min(1)] private int dropQuantity = 1;
    [SerializeField] private bool dropOutsideSlotsWhenPanelIsRootCanvas = true;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursorWhenVisible = true;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float iconInset = 18f;
    [SerializeField] private Color iconTint = Color.white;
    [SerializeField] private Color fallbackIconTint = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] private Color draggedSlotTint = new Color(1f, 1f, 1f, 0.45f);

    private readonly List<SlotView> slotViews = new List<SlotView>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private Inventory subscribedInventory;
    private Image dragIcon;
    private RectTransform dragIconRect;
    private int draggingSlotIndex = -1;
    private bool isDraggingSlot;
    private bool cursorStateCaptured;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    public Inventory Inventory => inventory;

    public static bool IsAnyInventoryOpen
    {
        get
        {
            InventoryCanvasUI[] inventoryUis = FindObjectsByType<InventoryCanvasUI>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < inventoryUis.Length; i++)
            {
                if (inventoryUis[i] != null && inventoryUis[i].IsVisible())
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Reset()
    {
        ResolveCanvasReferences();
        ResolveSlotRoots();
    }

    private void Awake()
    {
        ResolveCanvasReferences();
        ResolveSlotRoots();
        EnsureSlotViews();
        TryBindInventory();
        Refresh();
    }

    private void OnEnable()
    {
        SubscribeToInventory(inventory);
        Refresh();
    }

    private void OnDisable()
    {
        SubscribeToInventory(null);
        EndCursorOverride();
        HideDragIcon();
        isDraggingSlot = false;
        draggingSlotIndex = -1;
    }

    private void Update()
    {
        if (autoBindRuntimeInventory && inventory == null)
        {
            TryBindInventory();
        }

        ApplyCursorState(IsVisible());
    }

    public void Refresh()
    {
        EnsureSlotViews();

        for (int i = 0; i < slotViews.Count; i++)
        {
            InventorySlot slot = inventory != null ? inventory.GetSlot(i) : null;
            SetSlotView(slotViews[i], slot);
        }
    }

    public void BeginDragSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
        if (slot == null || !slot.BelongsTo(this) || !HasItem(slot.SlotIndex))
        {
            return;
        }

        draggingSlotIndex = slot.SlotIndex;
        isDraggingSlot = true;
        ShowDragIcon(draggingSlotIndex, eventData);
        SetDraggedSlotTint(draggingSlotIndex, true);
    }

    public void DragSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
        if (!isDraggingSlot || slot == null || slot.SlotIndex != draggingSlotIndex)
        {
            return;
        }

        MoveDragIcon(eventData);
    }

    public void EndDragSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
        if (!isDraggingSlot || slot == null || slot.SlotIndex != draggingSlotIndex)
        {
            return;
        }

        int sourceSlotIndex = draggingSlotIndex;
        bool pointerOverSlot = TryGetSlotUnderPointer(eventData, out int targetSlotIndex);
        bool pointerInsideInventory = pointerOverSlot || IsPointerInsideInventory(eventData);

        HideDragIcon();
        SetDraggedSlotTint(sourceSlotIndex, false);
        isDraggingSlot = false;
        draggingSlotIndex = -1;

        if (inventory == null)
        {
            Refresh();
            return;
        }

        if (pointerOverSlot && targetSlotIndex != sourceSlotIndex)
        {
            inventory.MoveItem(sourceSlotIndex, targetSlotIndex);
        }
        else if (!pointerInsideInventory)
        {
            DropSlot(sourceSlotIndex);
        }
        else
        {
            Refresh();
        }
    }

    public void DropSlot(int slotIndex)
    {
        if (inventory == null)
        {
            return;
        }

        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty)
        {
            Refresh();
            return;
        }

        int quantity = dropEntireStackWhenDraggedOutside
            ? slot.quantity
            : Mathf.Min(dropQuantity, slot.quantity);

        if (!inventory.DropItem(slotIndex, quantity))
        {
            Refresh();
        }
    }

    public bool TryGetSlotIndex(InventoryCanvasSlot slot, out int slotIndex)
    {
        slotIndex = -1;
        if (slot == null || !slot.BelongsTo(this))
        {
            return false;
        }

        slotIndex = slot.SlotIndex;
        return slotIndex >= 0 && slotIndex < slotViews.Count;
    }

    public bool TryPlaceExternalSlot(int slotIndex, InventorySlot incomingSlot, out InventorySlot swappedSlot)
    {
        swappedSlot = null;
        if (inventory == null || incomingSlot == null || incomingSlot.IsEmpty)
        {
            return false;
        }

        InventorySlot targetSlot = inventory.GetSlot(slotIndex);
        if (targetSlot == null)
        {
            return false;
        }

        swappedSlot = new InventorySlot();
        swappedSlot.CopyFrom(targetSlot);
        targetSlot.CopyFrom(incomingSlot);
        inventory.NotifyInventoryChanged();
        Refresh();
        return true;
    }

    public void ShowTooltip(InventorySlot slot)
    {
    }

    public void HideTooltip()
    {
    }

    private bool TryBindInventory()
    {
        if (inventoryOwner != null)
        {
            Inventory ownerInventory = inventoryOwner.Inventory != null
                ? inventoryOwner.Inventory
                : inventoryOwner.GetComponent<Inventory>();

            SetInventory(ownerInventory);
            return inventory != null;
        }

        if (inventory != null)
        {
            SubscribeToInventory(inventory);
            return true;
        }

        PlayerInventoryOwner foundOwner = FindPreferredInventoryOwner();
        if (foundOwner != null)
        {
            inventoryOwner = foundOwner;
            SetInventory(foundOwner.Inventory != null ? foundOwner.Inventory : foundOwner.GetComponent<Inventory>());
            return inventory != null;
        }

        SetInventory(FindFirstObjectByType<Inventory>());
        return inventory != null;
    }

    private PlayerInventoryOwner FindPreferredInventoryOwner()
    {
        PlayerInventoryOwner[] owners = FindObjectsByType<PlayerInventoryOwner>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        PlayerInventoryOwner fallback = null;

        foreach (PlayerInventoryOwner owner in owners)
        {
            if (owner == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = owner;
            }

            if (owner.GetComponent<PlayerController>() != null)
            {
                return owner;
            }
        }

        return fallback;
    }

    private void SetInventory(Inventory target)
    {
        if (inventory == target && subscribedInventory == target)
        {
            return;
        }

        inventory = target;
        SubscribeToInventory(inventory);
        Refresh();
    }

    private void SubscribeToInventory(Inventory target)
    {
        if (subscribedInventory == target)
        {
            return;
        }

        if (subscribedInventory != null)
        {
            subscribedInventory.OnInventoryChanged -= Refresh;
        }

        subscribedInventory = target;

        if (subscribedInventory != null && isActiveAndEnabled)
        {
            subscribedInventory.OnInventoryChanged += Refresh;
        }
    }

    private bool HasItem(int slotIndex)
    {
        InventorySlot slot = inventory != null ? inventory.GetSlot(slotIndex) : null;
        return slot != null && !slot.IsEmpty && slot.quantity > 0;
    }

    private void EnsureSlotViews()
    {
        ResolveCanvasReferences();
        ResolveSlotRoots();

        if (slotViews.Count == slotRoots.Length)
        {
            return;
        }

        slotViews.Clear();

        for (int i = 0; i < slotRoots.Length; i++)
        {
            RectTransform root = slotRoots[i];
            if (root == null)
            {
                continue;
            }

            InventoryCanvasSlot slotComponent = root.GetComponent<InventoryCanvasSlot>();
            if (slotComponent == null)
            {
                slotComponent = root.gameObject.AddComponent<InventoryCanvasSlot>();
            }

            slotComponent.Initialize(this, i);

            SlotView view = new SlotView
            {
                root = root,
                slot = slotComponent,
                icon = EnsureIcon(root),
                countText = EnsureCountText(root)
            };

            slotViews.Add(view);
        }
    }

    private Image EnsureIcon(RectTransform slotRoot)
    {
        Transform existing = slotRoot.Find(RuntimeIconName);
        GameObject iconObject = existing != null ? existing.gameObject : new GameObject(RuntimeIconName, typeof(RectTransform));
        iconObject.transform.SetParent(slotRoot, false);

        Image image = iconObject.GetComponent<Image>();
        if (image == null)
        {
            image = iconObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.preserveAspect = true;

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(iconInset, iconInset);
        rect.offsetMax = new Vector2(-iconInset, -iconInset);
        rect.SetAsLastSibling();

        return image;
    }

    private TMP_Text EnsureCountText(RectTransform slotRoot)
    {
        Transform existing = slotRoot.Find(RuntimeCountName);
        GameObject countObject = existing != null ? existing.gameObject : new GameObject(RuntimeCountName, typeof(RectTransform));
        countObject.transform.SetParent(slotRoot, false);

        TextMeshProUGUI text = countObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = countObject.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.18f;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6f, 4f);
        rect.offsetMax = new Vector2(-7f, -4f);
        rect.SetAsLastSibling();

        return text;
    }

    private void SetSlotView(SlotView view, InventorySlot slot)
    {
        bool hasItem = slot != null && !slot.IsEmpty && slot.quantity > 0;
        Sprite icon = hasItem && slot.itemData != null ? slot.itemData.icon : null;

        if (view.icon != null)
        {
            view.icon.gameObject.SetActive(hasItem);
            view.icon.sprite = icon;
            view.icon.color = icon != null ? iconTint : fallbackIconTint;
        }

        if (view.countText != null)
        {
            view.countText.text = hasItem && slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
            view.countText.gameObject.SetActive(hasItem && slot.quantity > 1);
        }
    }

    private void ShowDragIcon(int slotIndex, PointerEventData eventData)
    {
        InventorySlot slot = inventory != null ? inventory.GetSlot(slotIndex) : null;
        if (slot == null || slot.IsEmpty)
        {
            return;
        }

        EnsureDragIcon();
        if (dragIcon == null)
        {
            return;
        }

        dragIcon.sprite = slot.itemData != null ? slot.itemData.icon : null;
        dragIcon.color = dragIcon.sprite != null ? iconTint : fallbackIconTint;
        dragIcon.gameObject.SetActive(true);
        dragIcon.transform.SetAsLastSibling();

        RectTransform slotRoot = slotIndex >= 0 && slotIndex < slotViews.Count ? slotViews[slotIndex].root : null;
        if (slotRoot != null && dragIconRect != null)
        {
            float size = Mathf.Max(28f, Mathf.Min(slotRoot.rect.width, slotRoot.rect.height) - iconInset * 2f);
            dragIconRect.sizeDelta = new Vector2(size, size);
        }

        MoveDragIcon(eventData);
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        if (dragIconRect != null && eventData != null)
        {
            dragIconRect.position = eventData.position;
        }
    }

    private void HideDragIcon()
    {
        if (dragIcon != null)
        {
            dragIcon.gameObject.SetActive(false);
            dragIcon.sprite = null;
        }
    }

    private void EnsureDragIcon()
    {
        if (dragIcon != null)
        {
            return;
        }

        ResolveCanvasReferences();
        Transform parent = rootCanvas != null ? rootCanvas.transform : transform;
        GameObject dragObject = new GameObject(RuntimeDragIconName, typeof(RectTransform));
        dragObject.transform.SetParent(parent, false);

        dragIcon = dragObject.AddComponent<Image>();
        dragIcon.raycastTarget = false;
        dragIcon.preserveAspect = true;

        CanvasGroup dragCanvasGroup = dragObject.AddComponent<CanvasGroup>();
        dragCanvasGroup.blocksRaycasts = false;
        dragCanvasGroup.interactable = false;

        dragIconRect = dragObject.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(64f, 64f);
        dragObject.SetActive(false);
    }

    private void SetDraggedSlotTint(int slotIndex, bool dragging)
    {
        if (slotIndex < 0 || slotIndex >= slotViews.Count)
        {
            return;
        }

        Image icon = slotViews[slotIndex].icon;
        if (icon != null && icon.gameObject.activeSelf)
        {
            icon.color = dragging ? draggedSlotTint : iconTint;
        }
    }

    private bool TryGetSlotUnderPointer(PointerEventData eventData, out int slotIndex)
    {
        slotIndex = -1;
        if (eventData == null)
        {
            return false;
        }

        if (EventSystem.current != null)
        {
            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (RaycastResult result in raycastResults)
            {
                InventoryCanvasSlot slot = result.gameObject.GetComponentInParent<InventoryCanvasSlot>();
                if (slot != null && slot.BelongsTo(this))
                {
                    slotIndex = slot.SlotIndex;
                    return true;
                }
            }
        }

        Camera eventCamera = GetEventCamera(eventData);
        for (int i = 0; i < slotViews.Count; i++)
        {
            RectTransform root = slotViews[i].root;
            if (root != null && RectTransformUtility.RectangleContainsScreenPoint(root, eventData.position, eventCamera))
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool IsPointerInsideInventory(PointerEventData eventData)
    {
        if (inventoryPanel == null || eventData == null)
        {
            return false;
        }

        if (dropOutsideSlotsWhenPanelIsRootCanvas && rootCanvas != null && inventoryPanel == rootCanvas.transform)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            inventoryPanel,
            eventData.position,
            GetEventCamera(eventData));
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (eventData != null && eventData.pressEventCamera != null)
        {
            return eventData.pressEventCamera;
        }

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return rootCanvas.worldCamera;
        }

        return null;
    }

    private bool IsVisible()
    {
        bool canvasVisible = rootCanvas == null || rootCanvas.enabled;
        bool groupVisible = canvasGroup == null || (canvasGroup.alpha > 0.001f && canvasGroup.blocksRaycasts);
        return isActiveAndEnabled && canvasVisible && groupVisible;
    }

    private void ApplyCursorState(bool visible)
    {
        if (!unlockCursorWhenVisible)
        {
            return;
        }

        if (visible)
        {
            if (!cursorStateCaptured)
            {
                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                cursorStateCaptured = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        EndCursorOverride();
    }

    private void EndCursorOverride()
    {
        if (!cursorStateCaptured)
        {
            return;
        }

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        cursorStateCaptured = false;
    }

    private void ResolveCanvasReferences()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = transform as RectTransform;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void ResolveSlotRoots()
    {
        if (slotRoots != null && slotRoots.Length > 0)
        {
            return;
        }

        List<RectTransform> found = new List<RectTransform>();
        foreach (Transform child in transform)
        {
            RectTransform childRect = child as RectTransform;
            if (childRect != null && child.name.StartsWith("Slot "))
            {
                found.Add(childRect);
            }
        }

        found.Sort((left, right) => GetSlotNumber(left.name).CompareTo(GetSlotNumber(right.name)));
        slotRoots = found.ToArray();
    }

    private static int GetSlotNumber(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
        {
            return int.MaxValue;
        }

        string[] parts = slotName.Split(' ');
        if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int number))
        {
            return number;
        }

        return int.MaxValue;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        dropQuantity = Mathf.Max(1, dropQuantity);
        ResolveCanvasReferences();
        ResolveSlotRoots();
    }
#endif

    private sealed class SlotView
    {
        public RectTransform root;
        public InventoryCanvasSlot slot;
        public Image icon;
        public TMP_Text countText;
    }
}
