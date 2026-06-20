using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {
    [Header("Inventory")] public Inventory inventory;

    [Header("Panel")] public GameObject inventoryPanel;
    public KeyCode toggleKey = KeyCode.Tab;
    public KeyCode dropHoveredKey = KeyCode.Q;
    public bool startOpen;
    public bool unlockCursorWhenOpen = true;

    [Header("Slots")] public Transform slotsContainer;
    public InventorySlotUI slotPrefab;

    [Header("Tooltip")] public GameObject tooltipPanel;
    public TMP_Text tooltipNameText;
    public TMP_Text tooltipDescriptionText;

    [Header("Drag")] public Image dragIcon;
    public TMP_Text dragCountText;

    private const float DoubleClickMaxDelay = 0.35f;
    private static int _openInventoryCount;

    private readonly List<InventorySlotUI> _slotViews = new List<InventorySlotUI>();
    private Inventory _subscribedInventory;
    private int _hoveredSlotIndex = -1;
    private int _lastClickedSlotIndex = -1;
    private float _lastClickTime = -10f;
    private ItemData _carriedItem;
    private int _carriedQuantity;
    private PickupDropVisual _carriedVisual;
    private bool _isRegisteredOpen;
    private bool _hasStoredCursorState;
    private CursorLockMode _previousCursorLockMode;
    private bool _previousCursorVisible;

    public static bool IsAnyInventoryOpen => _openInventoryCount > 0;
    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;
    private bool HasCarriedItem => _carriedItem != null && _carriedQuantity > 0;

    private void Awake() {
        TryBindInventory();
        HideTemplateSlot();
        EnsureDragIcon();
        SetOpen(startOpen);
        HideTooltip();
        RebuildSlots();
    }

    private void OnEnable() {
        SubscribeToInventory(inventory);
    }

    private void OnDisable() {
        SubscribeToInventory(null);
        SetOpen(false);
        ClearCarriedItem();
    }

    private void Update() {
        if (!inventory && TryBindInventory()) {
            RebuildSlots();
            Refresh();
        }

        if (InputKeyUtils.WasPressedThisFrame(toggleKey)) {
            Toggle();
        }

        if (!IsOpen) {
            return;
        }

        HandlePointerInput();

        if (_hoveredSlotIndex >= 0 && InputKeyUtils.WasPressedThisFrame(dropHoveredKey)) {
            DropSlot(_hoveredSlotIndex);
        }

        UpdateCarriedIconPosition();
    }

    public void Toggle() {
        SetOpen(!inventoryPanel || !inventoryPanel.activeSelf);
    }

    public void SetOpen(bool isOpen) {
        if (inventoryPanel != null) {
            inventoryPanel.SetActive(isOpen);
        }

        bool isNowOpen = IsOpen;
        if (_isRegisteredOpen != isNowOpen) {
            _openInventoryCount += isNowOpen ? 1 : -1;
            _openInventoryCount = Mathf.Max(0, _openInventoryCount);
            _isRegisteredOpen = isNowOpen;
        }

        ApplyCursorState(isNowOpen);

        if (!isOpen) {
            _hoveredSlotIndex = -1;
            HideTooltip();
            ClearCarriedItem();
        }
    }

    public void DropSlot(int slotIndex) {
        if (!inventory) {
            return;
        }

        bool keepHover = _hoveredSlotIndex == slotIndex;
        if (inventory.DropItem(slotIndex)) {
            Refresh();

            if (keepHover && IsSlotOccupied(slotIndex)) {
                _hoveredSlotIndex = slotIndex;
                ShowTooltip(slotIndex);
            }
            else {
                _hoveredSlotIndex = -1;
                HideTooltip();
            }
        }
    }

    public void ShowTooltip(int slotIndex) {
        _hoveredSlotIndex = slotIndex;

        if (tooltipPanel is null || !inventory || slotIndex < 0 || slotIndex >= inventory.slots.Count) {
            HideTooltip();
            return;
        }

        InventorySlot slot = inventory.slots[slotIndex];
        if (slot == null || slot.IsEmpty || !slot.itemData || HasCarriedItem) {
            HideTooltip();
            return;
        }

        if (tooltipNameText != null) {
            tooltipNameText.text = string.IsNullOrWhiteSpace(slot.itemData.itemName)
                ? "Item"
                : slot.itemData.itemName;
        }

        if (tooltipDescriptionText) {
            tooltipDescriptionText.text = slot.itemData.description ?? string.Empty;
        }

        tooltipPanel.SetActive(true);
    }

    public void HideTooltip() {
        if (tooltipPanel) {
            tooltipPanel.SetActive(false);
        }
    }

    public void ClearHoveredSlot(int slotIndex) {
        if (_hoveredSlotIndex == slotIndex) {
            _hoveredSlotIndex = -1;
            HideTooltip();
        }
    }

    public void Refresh() {
        if (!inventory) {
            return;
        }

        if (_slotViews.Count != inventory.slots.Count) {
            RebuildSlots();
        }

        for (int i = 0; i < _slotViews.Count; i++) {
            InventorySlot slot = i < inventory.slots.Count ? inventory.slots[i] : null;
            _slotViews[i].SetSlot(slot);
        }

        UpdateCarriedIcon();
    }

    private void HandlePointerInput() {
        if (Mouse.current == null) {
            return;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        if ((Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            && IsPointerOverDropButton(pointerPosition)) {
            return;
        }

        bool hasSlot = TryGetSlotUnderPointer(pointerPosition, out int slotIndex);

        if (Mouse.current.leftButton.wasPressedThisFrame) {
            if (hasSlot) {
                bool isDoubleClick = _lastClickedSlotIndex == slotIndex &&
                                     Time.unscaledTime - _lastClickTime <= DoubleClickMaxDelay;
                _lastClickedSlotIndex = slotIndex;
                _lastClickTime = Time.unscaledTime;

                if (!isDoubleClick || HasCarriedItem || !CollectStack(slotIndex)) {
                    LeftClickSlot(slotIndex);
                }
            }
            else if (HasCarriedItem) {
                if (IsPointerInsideInventoryPanel(pointerPosition)) {
                    int nearestSlot = GetNearestSlotIndex(pointerPosition);
                    if (nearestSlot >= 0) {
                        LeftClickSlot(nearestSlot);
                    }
                }
                else {
                    DropCarriedOutsideInventory(_carriedQuantity);
                }
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame) {
            if (hasSlot) {
                RightClickSlot(slotIndex);
            }
            else if (HasCarriedItem && !IsPointerInsideInventoryPanel(pointerPosition)) {
                DropCarriedOutsideInventory(1);
            }
        }
    }

    private void LeftClickSlot(int slotIndex) {
        if (!inventory) {
            return;
        }

        if (!HasCarriedItem) {
            TakeFromSlot(slotIndex, int.MaxValue);
            return;
        }

        InventorySlot targetSlot = inventory.GetSlot(slotIndex);
        if (targetSlot == null) {
            return;
        }

        if (targetSlot.IsEmpty || CanMergeCarriedInto(targetSlot)) {
            PlaceCarriedIntoSlot(slotIndex, _carriedQuantity);
            return;
        }

        SwapCarriedWithSlot(slotIndex);
    }

    private void RightClickSlot(int slotIndex) {
        if (!inventory) {
            return;
        }

        if (!HasCarriedItem) {
            InventorySlot slot = inventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty) {
                return;
            }

            int halfQuantity = Mathf.CeilToInt(slot.quantity / 2f);
            TakeFromSlot(slotIndex, halfQuantity);
            return;
        }

        PlaceCarriedIntoSlot(slotIndex, 1);
    }

    private void TakeFromSlot(int slotIndex, int requestedQuantity) {
        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) {
            return;
        }

        int quantity = Mathf.Min(requestedQuantity, slot.quantity);
        _carriedItem = slot.itemData;
        _carriedQuantity = quantity;
        _carriedVisual = slot.dropVisual?.Copy();

        slot.quantity -= quantity;
        if (slot.quantity <= 0) {
            slot.Clear();
        }

        inventory.NotifyInventoryChanged();
        HideTooltip();
        UpdateCarriedIcon();
    }

    private void PlaceCarriedIntoSlot(int slotIndex, int requestedQuantity) {
        if (!HasCarriedItem) {
            return;
        }

        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot == null) {
            return;
        }

        int quantity = Mathf.Min(requestedQuantity, _carriedQuantity);
        if (quantity <= 0) {
            return;
        }

        if (slot.IsEmpty) {
            slot.SetItem(_carriedItem, quantity, _carriedVisual);
            ReduceCarried(quantity);
            inventory.NotifyInventoryChanged();
            return;
        }

        if (!CanMergeCarriedInto(slot)) {
            return;
        }

        int available = inventory.GetStackLimitFor(slot.itemData) - slot.quantity;
        int placed = Mathf.Min(quantity, available);
        if (placed <= 0) {
            return;
        }

        slot.quantity += placed;
        ReduceCarried(placed);
        inventory.NotifyInventoryChanged();
    }

    private void SwapCarriedWithSlot(int slotIndex) {
        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || !HasCarriedItem) {
            return;
        }

        ItemData oldItem = slot.itemData;
        int oldQuantity = slot.quantity;
        PickupDropVisual oldVisual = slot.dropVisual?.Copy();

        slot.SetItem(_carriedItem, _carriedQuantity, _carriedVisual);
        _carriedItem = oldItem;
        _carriedQuantity = oldQuantity;
        _carriedVisual = oldVisual;

        inventory.NotifyInventoryChanged();
        UpdateCarriedIcon();
    }

    private bool CollectStack(int slotIndex) {
        if (!inventory || HasCarriedItem) {
            return false;
        }

        InventorySlot targetSlot = inventory.GetSlot(slotIndex);
        if (targetSlot == null || targetSlot.IsEmpty || !targetSlot.itemData.isStackable) {
            return false;
        }

        int limit = inventory.GetStackLimitFor(targetSlot.itemData);
        if (targetSlot.quantity >= limit) {
            return false;
        }

        int totalMoved = 0;
        for (int i = 0; i < inventory.slots.Count && targetSlot.quantity < limit; i++) {
            if (i == slotIndex) {
                continue;
            }

            InventorySlot sourceSlot = inventory.GetSlot(i);
            if (sourceSlot == null || sourceSlot.IsEmpty || sourceSlot.itemData != targetSlot.itemData) {
                continue;
            }

            int moved = Mathf.Min(limit - targetSlot.quantity, sourceSlot.quantity);
            if (moved <= 0) {
                continue;
            }

            targetSlot.quantity += moved;
            sourceSlot.quantity -= moved;
            totalMoved += moved;

            if (sourceSlot.quantity <= 0) {
                sourceSlot.Clear();
            }
        }

        if (totalMoved <= 0) {
            return false;
        }

        inventory.NotifyInventoryChanged();
        return true;
    }

    private void DropCarriedOutsideInventory(int quantity) {
        if (!inventory || !HasCarriedItem) {
            return;
        }

        int dropQuantity = Mathf.Clamp(quantity, 1, _carriedQuantity);
        if (inventory.DropLooseItem(_carriedItem, dropQuantity, _carriedVisual)) {
            ReduceCarried(dropQuantity);
            Refresh();
        }
    }

    private void ReduceCarried(int quantity) {
        _carriedQuantity -= quantity;
        if (_carriedQuantity <= 0) {
            ClearCarriedItem();
        }
        else {
            UpdateCarriedIcon();
        }
    }

    private bool CanMergeCarriedInto(InventorySlot slot) {
        return HasCarriedItem
               && slot != null
               && !slot.IsEmpty
               && slot.itemData == _carriedItem
               && slot.itemData.isStackable
               && slot.quantity < inventory.GetStackLimitFor(slot.itemData);
    }

    private void RebuildSlots() {
        ClearRuntimeSlots();

        if (!inventory || !slotsContainer || !slotPrefab) {
            return;
        }

        HideTemplateSlot();

        for (int i = 0; i < inventory.slots.Count; i++) {
            InventorySlotUI slotView = Instantiate(slotPrefab, slotsContainer);
            slotView.gameObject.name = $"Slot {i + 1}";
            slotView.gameObject.SetActive(true);
            slotView.Initialize(this, i);
            slotView.SetSlot(inventory.slots[i]);
            _slotViews.Add(slotView);
        }
    }

    private void ClearRuntimeSlots() {
        foreach (InventorySlotUI slotView in _slotViews) {
            if (slotView != null) {
                Destroy(slotView.gameObject);
            }
        }

        _slotViews.Clear();
    }

    private void HideTemplateSlot() {
        if (slotPrefab != null && slotsContainer != null && slotPrefab.transform.IsChildOf(slotsContainer)) {
            slotPrefab.gameObject.SetActive(false);
        }
    }

    private bool TryBindInventory() {
        if (inventory != null) {
            SubscribeToInventory(inventory);
            return true;
        }

        inventory = FindFirstObjectByType<Inventory>();
        SubscribeToInventory(inventory);
        return inventory;
    }

    private void SubscribeToInventory(Inventory target) {
        if (_subscribedInventory == target) {
            return;
        }

        if (_subscribedInventory is not null) {
            _subscribedInventory.OnInventoryChanged -= Refresh;
        }

        _subscribedInventory = target;

        if (_subscribedInventory && isActiveAndEnabled) {
            _subscribedInventory.OnInventoryChanged += Refresh;
        }
    }

    private bool IsSlotOccupied(int slotIndex) {
        InventorySlot slot = inventory != null ? inventory.GetSlot(slotIndex) : null;
        return slot != null && !slot.IsEmpty && slot.quantity > 0;
    }

    private void EnsureDragIcon() {
        if (dragIcon != null && slotPrefab != null && dragIcon.transform.IsChildOf(slotPrefab.transform)) {
            dragIcon = null;
            dragCountText = null;
        }

        if (dragIcon != null) {
            dragIcon.raycastTarget = false;
            EnsureDragCountText();
            dragIcon.gameObject.SetActive(false);
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) {
            return;
        }

        GameObject dragIconObject = new GameObject("Drag Icon");
        dragIconObject.transform.SetParent(canvas.transform, false);
        dragIcon = dragIconObject.AddComponent<Image>();
        dragIcon.raycastTarget = false;

        RectTransform rect = dragIconObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(56f, 56f);

        EnsureDragCountText();
        dragIconObject.SetActive(false);
    }

    private void EnsureDragCountText() {
        if (!dragIcon) {
            return;
        }

        if (!dragCountText || !dragCountText.transform.IsChildOf(dragIcon.transform)) {
            var countObject = new GameObject("Count");

            countObject.transform.SetParent(dragIcon.transform, false);
            dragCountText = countObject.AddComponent<TextMeshProUGUI>();
        }

        ApplyDragCountTemplateLayout();
        dragCountText.color = Color.black;
        dragCountText.outlineWidth = 0.22f;
        dragCountText.outlineColor = Color.black;
        dragCountText.raycastTarget = false;
        dragCountText.transform.SetAsLastSibling();
        dragCountText.gameObject.SetActive(false);
    }

    private void ApplyDragCountTemplateLayout() {
        if (dragCountText == null) {
            return;
        }

        RectTransform countRect = dragCountText.rectTransform;
        TMP_Text templateText = slotPrefab != null ? slotPrefab.countText : null;
        if (templateText != null) {
            RectTransform templateRect = templateText.rectTransform;
            countRect.anchorMin = templateRect.anchorMin;
            countRect.anchorMax = templateRect.anchorMax;
            countRect.pivot = templateRect.pivot;
            countRect.anchoredPosition = templateRect.anchoredPosition;
            countRect.sizeDelta = templateRect.sizeDelta;

            dragCountText.alignment = templateText.alignment;
            dragCountText.fontSize = templateText.fontSize;
            dragCountText.fontStyle = templateText.fontStyle | FontStyles.Bold;
            return;
        }

        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = new Vector2(2f, 2f);
        countRect.offsetMax = new Vector2(-2f, -2f);
        dragCountText.alignment = TextAlignmentOptions.BottomRight;
        dragCountText.fontSize = 18f;
        dragCountText.fontStyle = FontStyles.Bold;
    }

    private void UpdateCarriedIcon() {
        EnsureDragIcon();

        if (dragIcon == null) {
            return;
        }

        if (!HasCarriedItem) {
            dragIcon.gameObject.SetActive(false);
            dragIcon.sprite = null;
            if (dragCountText != null) {
                dragCountText.text = string.Empty;
                dragCountText.gameObject.SetActive(false);
            }

            return;
        }

        dragIcon.sprite = _carriedItem != null ? _carriedItem.icon : null;
        dragIcon.color = dragIcon.sprite != null ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.24f);
        dragIcon.gameObject.SetActive(true);
        dragIcon.transform.SetAsLastSibling();

        if (dragCountText != null) {
            bool showCount = _carriedQuantity > 1;
            dragCountText.text = showCount ? _carriedQuantity.ToString() : string.Empty;
            dragCountText.gameObject.SetActive(showCount);
            dragCountText.transform.SetAsLastSibling();
        }

        UpdateCarriedIconPosition();
    }

    private void UpdateCarriedIconPosition() {
        if (!HasCarriedItem || dragIcon == null || Mouse.current == null) {
            return;
        }

        dragIcon.rectTransform.position = Mouse.current.position.ReadValue();
    }

    private void ClearCarriedItem() {
        _carriedItem = null;
        _carriedQuantity = 0;
        _carriedVisual = null;
        UpdateCarriedIcon();
    }

    private bool TryGetSlotUnderPointer(Vector2 pointerPosition, out int slotIndex) {
        slotIndex = -1;
        if (EventSystem.current == null) {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = pointerPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results) {
            InventorySlotUI slot = result.gameObject.GetComponentInParent<InventorySlotUI>();
            if (slot != null && _slotViews.Contains(slot)) {
                slotIndex = _slotViews.IndexOf(slot);
                return true;
            }
        }

        return false;
    }

    private bool IsPointerOverDropButton(Vector2 pointerPosition) {
        if (EventSystem.current == null) {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = pointerPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results) {
            InventorySlotUI slot = result.gameObject.GetComponentInParent<InventorySlotUI>();
            if (slot != null && slot.IsDropButtonObject(result.gameObject)) {
                return true;
            }
        }

        return false;
    }

    private bool IsPointerInsideInventoryPanel(Vector2 pointerPosition) {
        if (inventoryPanel == null) {
            return false;
        }

        RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
        return panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, pointerPosition);
    }

    private int GetNearestSlotIndex(Vector2 pointerPosition) {
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < _slotViews.Count; i++) {
            RectTransform rect = _slotViews[i].GetComponent<RectTransform>();
            if (rect == null) {
                continue;
            }

            float distance = ((Vector2)rect.position - pointerPosition).sqrMagnitude;
            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void ApplyCursorState(bool inventoryOpen) {
        if (!unlockCursorWhenOpen) {
            return;
        }

        if (inventoryOpen) {
            if (!_hasStoredCursorState) {
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _hasStoredCursorState = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (_hasStoredCursorState) {
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
            _hasStoredCursorState = false;
        }
    }
}