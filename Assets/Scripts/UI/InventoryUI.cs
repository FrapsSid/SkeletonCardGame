using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {
    [Header("Inventory")] public Inventory inventory;
    public PlayerHand hand;

    [Header("Panel")] public GameObject inventoryPanel;
    public KeyCode toggleKey = KeyCode.Tab;
    public KeyCode dropHoveredKey = KeyCode.Q;
    public KeyCode moveHoveredToHandKey = KeyCode.F;
    public bool startOpen;
    public bool unlockCursorWhenOpen = true;

    [Header("Slots")]
    public Transform slotsContainer;
    public InventorySlotUI dragTemplateSlot;

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
    private CardData _carriedCardData;
    private bool _isRegisteredOpen;
    private bool _hasStoredCursorState;
    private CursorLockMode _previousCursorLockMode;
    private bool _previousCursorVisible;
    private PlayerHandUI _handUi;

    public static bool IsAnyInventoryOpen => _openInventoryCount > 0;
    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;
    private bool HasCarriedItem => _carriedItem != null && _carriedQuantity > 0;

    private void Awake() {
        TryBindInventory();
        TryBindHand();
        CacheManualSlots();
        EnsureDragIcon();
        SetOpen(startOpen);
        HideTooltip();
        Refresh();
    }

    private void OnEnable() {
        SubscribeToInventory(inventory);
        Refresh();
    }

    private void OnDisable() {
        SubscribeToInventory(null);
        SetOpen(false);
        ClearCarriedItem();
    }

    private void Update() {
        if ((inventory == null || !inventory.gameObject.scene.IsValid()) && TryBindInventory()) {
            Refresh();
        }

        if (hand == null || !hand.gameObject.scene.IsValid()) {
            TryBindHand();
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

        if (_hoveredSlotIndex >= 0 && InputKeyUtils.WasPressedThisFrame(moveHoveredToHandKey)) {
            MoveSlotToHand(_hoveredSlotIndex);
        }

        UpdateCarriedIconPosition();
    }

    public void Toggle() {
        SetOpen(!inventoryPanel || !inventoryPanel.activeSelf);
    }

    public void SetOpen(bool isOpen) {
        if (inventoryPanel != null) {
            inventoryPanel.SetActive(isOpen);
            if (isOpen) {
                inventoryPanel.transform.SetAsLastSibling();
            }
        }

        bool isNowOpen = IsOpen;
        if (_isRegisteredOpen != isNowOpen) {
            _openInventoryCount += isNowOpen ? 1 : -1;
            _openInventoryCount = Mathf.Max(0, _openInventoryCount);
            _isRegisteredOpen = isNowOpen;
        }

        ApplyCursorState(isNowOpen);
        ApplyHandPanelState(isNowOpen);

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

    public bool TryGetSlotIndex(InventorySlotUI slotView, out int slotIndex) {
        slotIndex = -1;
        if (slotView == null) {
            return false;
        }

        for (int i = 0; i < _slotViews.Count; i++) {
            if (_slotViews[i] == slotView) {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    public bool TryPlaceExternalSlot(int slotIndex, InventorySlot incomingSlot, out InventorySlot swappedSlot) {
        swappedSlot = null;
        if (!inventory || incomingSlot == null || incomingSlot.IsEmpty) {
            return false;
        }

        if (incomingSlot.cardData != null || incomingSlot.itemData == null || incomingSlot.itemData.category == ItemCategory.Card) {
            return false;
        }

        InventorySlot targetSlot = inventory.GetSlot(slotIndex);
        if (targetSlot == null) {
            return false;
        }

        swappedSlot = new InventorySlot();
        swappedSlot.CopyFrom(targetSlot);
        targetSlot.CopyFrom(incomingSlot);
        inventory.NotifyInventoryChanged();
        return true;
    }

    public void MoveSlotToHand(int slotIndex) {
        if (!inventory) {
            return;
        }

        if (!hand && !TryBindHand()) {
            return;
        }

        if (hand != null && hand.TryTakeFromInventory(inventory, slotIndex)) {
            Refresh();
        }
    }

    public void ShowTooltip(InventorySlot slot) {
        if (tooltipPanel is null || slot == null || slot.IsEmpty || slot.itemData == null || HasCarriedItem) {
            HideTooltip();
            return;
        }

        if (tooltipNameText != null) {
            if (slot.cardData != null) {
                tooltipNameText.text = $"{slot.cardData.Value} of {slot.cardData.Suit}";
            }
            else {
                tooltipNameText.text = string.IsNullOrWhiteSpace(slot.itemData.itemName)
                    ? "Item"
                    : slot.itemData.itemName;
            }
        }

        if (tooltipDescriptionText) {
            tooltipDescriptionText.text = slot.itemData.description ?? string.Empty;
        }

        tooltipPanel.SetActive(true);
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

        ShowTooltip(slot);
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

        if (_slotViews.Count == 0) {
            CacheManualSlots();
        }

        int visibleCount = Mathf.Min(_slotViews.Count, inventory.slots.Count);
        for (int i = 0; i < _slotViews.Count; i++) {
            InventorySlot slot = i < visibleCount ? inventory.slots[i] : null;
            _slotViews[i].SetSlot(slot);
        }

        UpdateCarriedIcon();
    }

    private void CacheManualSlots() {
        EnsureSlotViews();
        _slotViews.Clear();

        if (slotsContainer == null) {
            return;
        }

        InventorySlotUI[] slotViews = slotsContainer.GetComponentsInChildren<InventorySlotUI>(true);
        for (int i = 0; i < slotViews.Length; i++) {
            InventorySlotUI slotView = slotViews[i];
            if (slotView == null) {
                continue;
            }

            slotView.Initialize(this, _slotViews.Count);
            _slotViews.Add(slotView);
        }

        if (dragTemplateSlot == null && _slotViews.Count > 0) {
            dragTemplateSlot = _slotViews[0];
        }

        if (inventory != null && _slotViews.Count != inventory.maxSlots) {
            Debug.LogWarning($"[InventoryUI] Manual slot count ({_slotViews.Count}) does not match inventory.maxSlots ({inventory.maxSlots}).", this);
        }
    }

    private void EnsureSlotViews() {
        if (slotsContainer == null || inventory == null) {
            return;
        }

        InventorySlotUI template = dragTemplateSlot;
        if (template == null) {
            template = slotsContainer.GetComponentInChildren<InventorySlotUI>(true);
        }

        if (template == null) {
            return;
        }

        InventorySlotUI[] existingSlots = slotsContainer.GetComponentsInChildren<InventorySlotUI>(true);
        int existingCount = existingSlots.Length;
        for (int i = existingCount; i < inventory.maxSlots; i++) {
            InventorySlotUI clone = Instantiate(template, slotsContainer);
            clone.name = $"Slot {i + 1}";
            RectTransform cloneRect = clone.transform as RectTransform;
            if (cloneRect != null) {
                cloneRect.localScale = Vector3.one;
            }
        }
    }

    private void ApplyHandPanelState(bool isInventoryOpen) {
        if (_handUi == null) {
            _handUi = FindFirstObjectByType<PlayerHandUI>();
        }

        if (_handUi == null) {
            return;
        }

        GameObject handPanel = _handUi.gameObject;
        if (handPanel != null && handPanel != inventoryPanel) {
            handPanel.SetActive(isInventoryOpen);
            if (isInventoryOpen) {
                handPanel.transform.SetAsLastSibling();
                inventoryPanel?.transform.SetAsLastSibling();
            }
        }
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

                if (HasCarriedItem) {
                    if (IsPointerInsideInventoryPanel(pointerPosition)) {
                        PlaceCarriedIntoSlot(slotIndex);
                    }
                    else {
                        DropCarriedOutsideInventory(_carriedQuantity);
                    }
                }
                else if (isDoubleClick) {
                    MoveSlotToHand(slotIndex);
                }
                else {
                    TakeFromSlot(slotIndex, int.MaxValue);
                }
            }
            else if (HasCarriedItem && TryPlaceCarriedIntoHand(pointerPosition)) {
                return;
            }
            else if (HasCarriedItem && TryGetHandSlotUnderPointer(pointerPosition, out _, out _)) {
                return;
            }
            else if (HasCarriedItem && !IsPointerInsideInventoryPanel(pointerPosition)) {
                DropCarriedOutsideInventory(1);
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame && hasSlot) {
            if (HasCarriedItem) {
                PlaceOneCarriedIntoSlot(slotIndex);
            }
            else {
                int halfQuantity = Mathf.Max(1, Mathf.CeilToInt(GetSlotQuantity(slotIndex) * 0.5f));
                TakeFromSlot(slotIndex, halfQuantity);
            }
        }
    }

    private bool TryPlaceCarriedIntoHand(Vector2 pointerPosition) {
        if (!HasCarriedItem) {
            return false;
        }

        if (_handUi == null) {
            _handUi = FindFirstObjectByType<PlayerHandUI>();
        }

        if (_handUi == null || !TryGetHandSlotUnderPointer(pointerPosition, out PlayerHandUI targetHandUi, out int handSlotIndex)) {
            return false;
        }

        InventorySlot incomingSlot = new InventorySlot();
        incomingSlot.SetItem(_carriedItem, _carriedQuantity, _carriedVisual, _carriedCardData);
        if (!targetHandUi.TryPlaceExternalSlot(handSlotIndex, incomingSlot, out InventorySlot swappedSlot)) {
            return false;
        }

        if (swappedSlot == null || swappedSlot.IsEmpty) {
            ClearCarriedItem();
        }
        else {
            _carriedItem = swappedSlot.itemData;
            _carriedQuantity = swappedSlot.quantity;
            _carriedVisual = swappedSlot.dropVisual?.Copy();
            _carriedCardData = swappedSlot.cardData;
            UpdateCarriedIcon();
        }

        Refresh();
        return true;
    }

    private bool TryGetHandSlotUnderPointer(Vector2 pointerPosition, out PlayerHandUI targetHandUi, out int slotIndex) {
        targetHandUi = null;
        slotIndex = -1;
        if (EventSystem.current == null) {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = pointerPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        for (int i = 0; i < results.Count; i++) {
            HandSlotUI slot = results[i].gameObject.GetComponentInParent<HandSlotUI>();
            if (slot == null) {
                continue;
            }

            PlayerHandUI owner = slot.GetComponentInParent<PlayerHandUI>();
            if (owner != null && owner.TryGetSlotIndex(slot, out slotIndex)) {
                targetHandUi = owner;
                return true;
            }
        }

        return false;
    }
    private void TakeFromSlot(int slotIndex, int requestedQuantity) {
        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.quantity <= 0) {
            return;
        }

        if (HasCarriedItem) {
            return;
        }

        int takenQuantity = Mathf.Clamp(requestedQuantity, 1, slot.quantity);
        _carriedItem = slot.itemData;
        _carriedQuantity = takenQuantity;
        _carriedVisual = slot.dropVisual?.Copy();
        _carriedCardData = slot.cardData;

        slot.quantity -= takenQuantity;
        if (slot.quantity <= 0) {
            slot.Clear();
        }

        inventory.NotifyInventoryChanged();
        ShowTooltip(slotIndex);
    }

    private void PlaceCarriedIntoSlot(int slotIndex) {
        if (!HasCarriedItem) {
            return;
        }

        InventorySlot targetSlot = inventory.GetSlot(slotIndex);
        if (targetSlot == null) {
            return;
        }

        if (targetSlot.IsEmpty) {
            targetSlot.SetItem(_carriedItem, _carriedQuantity, _carriedVisual, _carriedCardData);
            ClearCarriedItem();
            inventory.NotifyInventoryChanged();
            return;
        }

        if (!CanMergeCarriedInto(targetSlot)) {
            return;
        }

        int stackLimit = inventory.GetStackLimitFor(targetSlot.itemData);
        int available = stackLimit - targetSlot.quantity;
        int moved = Mathf.Min(available, _carriedQuantity);
        if (moved <= 0) {
            return;
        }

        targetSlot.quantity += moved;
        _carriedQuantity -= moved;
        if (_carriedQuantity <= 0) {
            ClearCarriedItem();
        }

        inventory.NotifyInventoryChanged();
    }

    private void PlaceOneCarriedIntoSlot(int slotIndex) {
        if (!HasCarriedItem) {
            return;
        }

        InventorySlot targetSlot = inventory.GetSlot(slotIndex);
        if (targetSlot == null) {
            return;
        }

        if (targetSlot.IsEmpty) {
            targetSlot.SetItem(_carriedItem, 1, _carriedVisual, _carriedCardData);
            _carriedQuantity -= 1;
            if (_carriedQuantity <= 0) {
                ClearCarriedItem();
            }

            inventory.NotifyInventoryChanged();
            return;
        }

        if (!CanMergeCarriedInto(targetSlot)) {
            return;
        }

        int stackLimit = inventory.GetStackLimitFor(targetSlot.itemData);
        if (targetSlot.quantity >= stackLimit) {
            return;
        }

        targetSlot.quantity += 1;
        _carriedQuantity -= 1;
        if (_carriedQuantity <= 0) {
            ClearCarriedItem();
        }

        inventory.NotifyInventoryChanged();
    }

    private void ClearCarriedItem() {
        _carriedItem = null;
        _carriedQuantity = 0;
        _carriedVisual = null;
        _carriedCardData = null;
        UpdateCarriedIcon();
    }

    private void UpdateCarriedIcon() {
        EnsureDragIcon();
        if (dragIcon == null) {
            return;
        }

        bool hasCarried = HasCarriedItem;
        dragIcon.enabled = hasCarried;
        dragIcon.sprite = hasCarried && _carriedItem != null ? _carriedItem.icon : null;
        dragIcon.color = !hasCarried ? Color.clear : dragIcon.sprite != null ? Color.white : new Color(0.82f, 0.86f, 0.92f, 0.9f);
        dragIcon.rectTransform.sizeDelta = new Vector2(64f, 64f);

        if (dragCountText != null) {
            dragCountText.text = hasCarried && _carriedQuantity > 1 ? _carriedQuantity.ToString() : string.Empty;
            dragCountText.gameObject.SetActive(hasCarried);
        }
    }

    private void UpdateCarriedIconPosition() {
        if (!HasCarriedItem || dragIcon == null || Mouse.current == null) {
            return;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        dragIcon.rectTransform.position = pointerPosition;
        if (dragCountText != null) {
            dragCountText.rectTransform.position = pointerPosition + new Vector2(18f, -18f);
        }
    }

    private void EnsureDragIcon() {
        if (dragIcon != null && inventoryPanel != null && !dragIcon.transform.IsChildOf(inventoryPanel.transform)) {
            dragIcon = null;
        }

        if (dragCountText != null && inventoryPanel != null && !dragCountText.transform.IsChildOf(inventoryPanel.transform)) {
            dragCountText = null;
        }

        if (dragIcon != null || inventoryPanel == null) {
            return;
        }

        GameObject dragObject = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragObject.transform.SetParent(inventoryPanel.transform, false);
        dragIcon = dragObject.GetComponent<Image>();
        dragIcon.raycastTarget = false;
        RectTransform dragRect = dragIcon.rectTransform;
        dragRect.sizeDelta = new Vector2(64f, 64f);

        if (dragCountText == null) {
            GameObject countObject = new GameObject("DragCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(inventoryPanel.transform, false);
            dragCountText = countObject.GetComponent<TextMeshProUGUI>();
            dragCountText.raycastTarget = false;
            dragCountText.fontSize = 24f;
            dragCountText.alignment = TextAlignmentOptions.BottomRight;
            dragCountText.color = Color.white;
        }

        UpdateCarriedIcon();
    }

    private void ApplyCursorState(bool isOpen) {
        if (!unlockCursorWhenOpen) {
            return;
        }

        if (isOpen) {
            if (!_hasStoredCursorState) {
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _hasStoredCursorState = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (_hasStoredCursorState) {
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
            _hasStoredCursorState = false;
        }
    }

    private void DropCarriedOutsideInventory(int quantity) {
        if (!HasCarriedItem || inventory == null) {
            return;
        }

        int dropQuantity = Mathf.Clamp(quantity, 1, _carriedQuantity);
        if (!inventory.DropLooseItem(_carriedItem, dropQuantity, _carriedVisual, _carriedCardData)) {
            return;
        }

        _carriedQuantity -= dropQuantity;
        if (_carriedQuantity <= 0) {
            ClearCarriedItem();
        }
        else {
            UpdateCarriedIcon();
        }
    }

    private bool CanMergeCarriedInto(InventorySlot slot) {
        return slot != null
               && !slot.IsEmpty
               && slot.cardData == null
               && _carriedCardData == null
               && slot.itemData == _carriedItem
               && _carriedItem != null
               && _carriedItem.isStackable;
    }

    private bool TryBindInventory() {
        if (inventory != null && inventory.gameObject.scene.IsValid()) {
            SubscribeToInventory(inventory);
            return true;
        }

        inventory = null;
        PlayerInventoryOwner owner = FindLocalInventoryOwner();
        if (owner != null) {
            inventory = owner.Inventory != null ? owner.Inventory : owner.GetComponent<Inventory>();
        }

        if (inventory == null) {
            inventory = FindFirstObjectByType<Inventory>();
        }

        SubscribeToInventory(inventory);
        return inventory != null;
    }

    private bool TryBindHand() {
        if (hand != null && hand.gameObject.scene.IsValid()) {
            return true;
        }

        hand = null;
        PlayerInventoryOwner owner = FindLocalInventoryOwner();
        if (owner != null) {
            hand = owner.Hand != null ? owner.Hand : owner.GetComponent<PlayerHand>();
        }

        if (hand == null) {
            hand = FindFirstObjectByType<PlayerHand>();
        }

        return hand != null;
    }

    private PlayerInventoryOwner FindLocalInventoryOwner() {
        PlayerInventoryOwner[] owners = FindObjectsOfType<PlayerInventoryOwner>();
        for (int i = 0; i < owners.Length; i++) {
            PlayerInventoryOwner owner = owners[i];
            if (owner == null || !owner.gameObject.scene.IsValid()) {
                continue;
            }

            if (owner.GetComponent<PlayerController>() != null) {
                return owner;
            }
        }

        return owners.Length > 0 ? owners[0] : null;
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

    private int GetSlotQuantity(int slotIndex) {
        InventorySlot slot = inventory != null ? inventory.GetSlot(slotIndex) : null;
        return slot != null ? Mathf.Max(0, slot.quantity) : 0;
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
        for (int i = 0; i < results.Count; i++) {
            RaycastResult result = results[i];
            InventorySlotUI slot = result.gameObject.GetComponentInParent<InventorySlotUI>();
            if (slot == null) {
                continue;
            }

            for (int j = 0; j < _slotViews.Count; j++) {
                if (_slotViews[j] == slot) {
                    slotIndex = j;
                    return true;
                }
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
        for (int i = 0; i < results.Count; i++) {
            RaycastResult result = results[i];
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

        RectTransform rectTransform = inventoryPanel.transform as RectTransform;
        if (rectTransform == null) {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition, null);
    }
}



