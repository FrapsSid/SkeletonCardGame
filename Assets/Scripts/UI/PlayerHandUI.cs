using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHandUI : MonoBehaviour {
    public PlayerHand hand;
    public InventoryCanvasUI inventoryUi;
    public Transform slotsContainer;
    public HandSlotUI slotPrefab;
    public CardAtlas cardAtlas;
    public int maxColumns = 4;
    public Vector2 slotSpacing = new Vector2(12f, 12f);

    [Header("Drag")]
    public Image dragIcon;
    public TMP_Text dragCountText;
    public Color fallbackItemColor = new Color(0.82f, 0.86f, 0.92f, 0.9f);

    private readonly List<HandSlotUI> _slotViews = new List<HandSlotUI>();
    private PlayerHand _subscribedHand;
    private readonly InventorySlot _carriedSlot = new InventorySlot();
    private int _hoveredSlotIndex = -1;

    private bool HasCarriedSlot => !_carriedSlot.IsEmpty && _carriedSlot.quantity > 0;

    private void Awake() {
        TryBindHand();
        TryBindInventoryUi();
        CacheManualSlots();
        EnsureDragIcon();
        Refresh();
    }

    private void OnEnable() {
        SubscribeToHand(hand);
        Refresh();
    }

    private void OnDisable() {
        SubscribeToHand(null);
        ClearCarriedSlot();
    }

    private void Update() {
        if ((hand == null || !hand.gameObject.scene.IsValid()) && TryBindHand()) {
            Refresh();
        }

        if (inventoryUi == null || !inventoryUi.gameObject.scene.IsValid()) {
            TryBindInventoryUi();
        }

        HandlePointerInput();
        UpdateCarriedIconPosition();
    }

    public void SelectSlot(int slotIndex) {
        if (hand != null) {
            hand.SelectSlot(slotIndex);
        }
    }

    public void Refresh() {
        if (hand == null) {
            return;
        }

        if (_slotViews.Count == 0) {
            CacheManualSlots();
        }

        int visibleCount = Mathf.Min(_slotViews.Count, hand.slots.Count);
        for (int i = 0; i < _slotViews.Count; i++) {
            InventorySlot slot = i < visibleCount ? hand.GetSlot(i) : null;
            _slotViews[i].SetSlot(slot, ResolveSprite(slot), hand.SelectedIndex == i);
        }

        UpdateCarriedIcon();
    }

    private void HandlePointerInput() {
        if (hand == null || Mouse.current == null || EventSystem.current == null) {
            return;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        UpdateTooltip(pointerPosition);

        if (InputKeyUtils.WasPressedThisFrame(KeyCode.Q) && TryGetHandSlotUnderPointer(pointerPosition, out int hoveredHandSlot)) {
            hand.TryDropSlot(hoveredHandSlot);
            Refresh();
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame) {
            return;
        }

        if (TryGetHandSlotUnderPointer(pointerPosition, out int handSlotIndex)) {
            if (IsShiftPressed() && !HasCarriedSlot) {
                if (inventoryUi != null) {
                    hand.TryMoveSlotToInventory(handSlotIndex, inventoryUi.Inventory);
                }

                Refresh();
                inventoryUi?.Refresh();
                return;
            }

            if (HasCarriedSlot) {
                PlaceCarriedIntoHandSlot(handSlotIndex);
            }
            else {
                TakeFromHandSlot(handSlotIndex);
            }

            return;
        }

        if (HasCarriedSlot && TryGetInventorySlotUnderPointer(pointerPosition, out InventoryCanvasUI targetInventoryUi, out int inventorySlotIndex)) {
            if (targetInventoryUi.TryPlaceExternalSlot(inventorySlotIndex, _carriedSlot, out InventorySlot swappedSlot)) {
                if (swappedSlot == null || swappedSlot.IsEmpty) {
                    ClearCarriedSlot();
                }
                else {
                    _carriedSlot.CopyFrom(swappedSlot);
                    UpdateCarriedIcon();
                }

                Refresh();
            }
        }
    }

    private void TakeFromHandSlot(int slotIndex) {
        if (hand == null) {
            return;
        }

        InventorySlot slot = hand.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.cardData != null || slot.itemData == null || slot.itemData.category == ItemCategory.Card) {
            return;
        }

        if (hand.TryTakeSlot(slotIndex, _carriedSlot)) {
            Refresh();
        }
    }

    private void PlaceCarriedIntoHandSlot(int slotIndex) {
        if (hand == null || !HasCarriedSlot) {
            return;
        }

        if (hand.TryPlaceSlot(slotIndex, _carriedSlot, out InventorySlot swappedSlot)) {
            if (swappedSlot == null || swappedSlot.IsEmpty) {
                ClearCarriedSlot();
            }
            else {
                _carriedSlot.CopyFrom(swappedSlot);
                UpdateCarriedIcon();
            }

            Refresh();
        }
    }

    private void ClearCarriedSlot() {
        _carriedSlot.Clear();
        UpdateCarriedIcon();
    }

    private void EnsureDragIcon() {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform dragParent = canvas != null ? canvas.transform : transform;

        if (dragIcon != null && !dragIcon.transform.IsChildOf(dragParent)) {
            dragIcon = null;
        }

        if (dragCountText != null && !dragCountText.transform.IsChildOf(dragParent)) {
            dragCountText = null;
        }

        if (dragIcon == null) {
            GameObject dragObject = new GameObject("HandDragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dragObject.transform.SetParent(dragParent, false);
            dragIcon = dragObject.GetComponent<Image>();
            dragIcon.raycastTarget = false;
        }

        dragIcon.rectTransform.sizeDelta = new Vector2(72f, 72f);

        if (dragCountText == null) {
            GameObject countObject = new GameObject("HandDragCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(dragParent, false);
            dragCountText = countObject.GetComponent<TextMeshProUGUI>();
            dragCountText.raycastTarget = false;
            dragCountText.fontSize = 24f;
            dragCountText.alignment = TextAlignmentOptions.BottomRight;
            dragCountText.color = Color.white;
        }
    }

    private void UpdateCarriedIcon() {
        EnsureDragIcon();
        if (dragIcon == null) {
            return;
        }

        Sprite sprite = ResolveSprite(_carriedSlot);
        dragIcon.enabled = HasCarriedSlot;
        dragIcon.sprite = sprite;
        dragIcon.color = !HasCarriedSlot ? Color.clear : sprite != null ? Color.white : fallbackItemColor;
        if (gameObject.activeInHierarchy) {
            dragIcon.transform.SetAsLastSibling();
        }

        if (dragCountText != null) {
            dragCountText.text = HasCarriedSlot && _carriedSlot.quantity > 1 && _carriedSlot.cardData == null
                ? _carriedSlot.quantity.ToString()
                : string.Empty;
            dragCountText.gameObject.SetActive(HasCarriedSlot);
            if (gameObject.activeInHierarchy) {
                dragCountText.transform.SetAsLastSibling();
            }
        }
    }

    private void UpdateCarriedIconPosition() {
        if (!HasCarriedSlot || dragIcon == null || Mouse.current == null) {
            return;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        dragIcon.rectTransform.position = pointerPosition;
        if (dragCountText != null) {
            dragCountText.rectTransform.position = pointerPosition + new Vector2(18f, -18f);
        }
    }

    private Sprite ResolveSprite(InventorySlot slot) {
        if (slot == null || slot.IsEmpty) {
            return null;
        }

        if (slot.cardData != null && cardAtlas != null) {
            return cardAtlas.GetFaceSprite(slot.cardData.Suit, slot.cardData.Value);
        }

        return slot.itemData != null ? slot.itemData.icon : null;
    }

    private void CacheManualSlots() {
        EnsureSlotViews();
        _slotViews.Clear();

        if (slotsContainer == null) {
            return;
        }

        HandSlotUI[] slotViews = slotsContainer.GetComponentsInChildren<HandSlotUI>(true);
        for (int i = 0; i < slotViews.Length; i++) {
            HandSlotUI slotView = slotViews[i];
            if (slotView == null) {
                continue;
            }

            slotView.Initialize(this, _slotViews.Count);
            _slotViews.Add(slotView);
        }

        if (hand != null && _slotViews.Count != hand.maxSlots) {
            Debug.LogWarning($"[PlayerHandUI] Manual slot count ({_slotViews.Count}) does not match hand.maxSlots ({hand.maxSlots}).", this);
        }
    }

    private void EnsureSlotViews() {
        if (slotsContainer == null || hand == null) {
            return;
        }

        HandSlotUI template = slotsContainer.GetComponentInChildren<HandSlotUI>(true);
        if (template == null) {
            template = slotPrefab;
        }

        if (template == null) {
            return;
        }

        HandSlotUI[] existingSlots = slotsContainer.GetComponentsInChildren<HandSlotUI>(true);
        int existingCount = existingSlots.Length;
        for (int i = existingCount; i < hand.maxSlots; i++) {
            HandSlotUI clone = Instantiate(template, slotsContainer);
            clone.name = $"HandSlot {i + 1}";
            RectTransform cloneRect = clone.transform as RectTransform;
            if (cloneRect != null) {
                cloneRect.localScale = Vector3.one;
            }
        }

        LayoutSlotViews();
    }

    private void LayoutSlotViews() {
        HandSlotUI[] slotViews = slotsContainer.GetComponentsInChildren<HandSlotUI>(true);
        if (slotViews.Length == 0) {
            return;
        }

        RectTransform firstRect = slotViews[0].transform as RectTransform;
        if (firstRect == null) {
            return;
        }

        int columnCount = Mathf.Max(1, Mathf.Min(maxColumns, slotViews.Length));
        int rowCount = Mathf.CeilToInt(slotViews.Length / (float)columnCount);
        float slotWidth = firstRect.rect.width;
        float slotHeight = firstRect.rect.height;
        float totalWidth = columnCount * slotWidth + (columnCount - 1) * slotSpacing.x;
        float totalHeight = rowCount * slotHeight + (rowCount - 1) * slotSpacing.y;
        float startX = -0.5f * totalWidth + 0.5f * slotWidth;
        float startY = 0.5f * totalHeight - 0.5f * slotHeight;

        for (int i = 0; i < slotViews.Length; i++) {
            RectTransform rect = slotViews[i].transform as RectTransform;
            if (rect == null) {
                continue;
            }

            int column = i % columnCount;
            int row = i / columnCount;
            rect.localScale = Vector3.one;
            rect.localPosition = new Vector3(
                startX + column * (slotWidth + slotSpacing.x),
                startY - row * (slotHeight + slotSpacing.y),
                0f);
        }
    }

    public bool TryGetSlotIndex(HandSlotUI slotView, out int slotIndex) {
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
        if (hand == null || incomingSlot == null || incomingSlot.IsEmpty) {
            return false;
        }

        if (!hand.TryPlaceSlot(slotIndex, incomingSlot, out swappedSlot)) {
            return false;
        }

        Refresh();
        return true;
    }

    private void UpdateTooltip(Vector2 pointerPosition) {
        if (HasCarriedSlot || inventoryUi == null) {
            return;
        }

        if (TryGetHandSlotUnderPointer(pointerPosition, out int slotIndex)) {
            InventorySlot slot = hand != null ? hand.GetSlot(slotIndex) : null;
            if (slot != null && !slot.IsEmpty) {
                _hoveredSlotIndex = slotIndex;
                inventoryUi.ShowTooltip(slot);
                return;
            }
        }

        if (_hoveredSlotIndex >= 0) {
            _hoveredSlotIndex = -1;
            inventoryUi.HideTooltip();
        }
    }
    private bool TryGetHandSlotUnderPointer(Vector2 pointerPosition, out int slotIndex) {
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

            for (int j = 0; j < _slotViews.Count; j++) {
                if (_slotViews[j] == slot) {
                    slotIndex = j;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetInventorySlotUnderPointer(Vector2 pointerPosition, out InventoryCanvasUI targetInventoryUi, out int slotIndex) {
        targetInventoryUi = null;
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
            InventoryCanvasSlot slot = results[i].gameObject.GetComponentInParent<InventoryCanvasSlot>();
            if (slot == null) {
                continue;
            }

            InventoryCanvasUI owner = slot.GetComponentInParent<InventoryCanvasUI>();
            if (owner != null && owner.TryGetSlotIndex(slot, out slotIndex)) {
                targetInventoryUi = owner;
                return true;
            }
        }

        return false;
    }

    private bool TryBindHand() {
        if (hand != null && hand.gameObject.scene.IsValid()) {
            SubscribeToHand(hand);
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

        SubscribeToHand(hand);
        return hand != null;
    }

    private bool TryBindInventoryUi() {
        if (inventoryUi != null && inventoryUi.gameObject.scene.IsValid()) {
            return true;
        }

        inventoryUi = FindFirstObjectByType<InventoryCanvasUI>();
        return inventoryUi != null;
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

    private void SubscribeToHand(PlayerHand target) {
        if (_subscribedHand == target) {
            return;
        }

        if (_subscribedHand != null) {
            _subscribedHand.OnHandChanged -= Refresh;
            _subscribedHand.OnSelectionChanged -= HandleSelectionChanged;
        }

        _subscribedHand = target;

        if (_subscribedHand != null && isActiveAndEnabled) {
            _subscribedHand.OnHandChanged += Refresh;
            _subscribedHand.OnSelectionChanged += HandleSelectionChanged;
        }
    }

    private void HandleSelectionChanged(int _) {
        Refresh();
    }

    private static bool IsShiftPressed() {
        return Keyboard.current != null
            ? Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed
            : Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
}



