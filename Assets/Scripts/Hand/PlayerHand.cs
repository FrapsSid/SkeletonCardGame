using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHand : MonoBehaviour {
    [Header("Capacity")] [Min(1)] public int maxSlots = 8;
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Card Settings")] public ItemData cardItemData;

    [Header("Dropping")] public GameObject pickupPrefab;
    public Vector3 dropOffset = new Vector3(0f, 0.4f, 1.05f);
    public bool giveDroppedItemForwardVelocity = true;
    public float droppedItemForwardVelocity = 1.5f;

    [Header("Held Visual")]
    public bool showSelectedItemInHand = true;
    public Transform heldItemAnchor;
    public Vector3 heldItemLocalPosition = new Vector3(0.35f, 1.15f, 0.55f);
    public Vector3 heldItemLocalEulerAngles = new Vector3(15f, 25f, 0f);
    public Vector3 heldItemLocalScale = new Vector3(0.35f, 0.35f, 0.35f);

    public event Action OnHandChanged;
    public event Action<int> OnSelectionChanged;

    public int SelectedIndex { get; private set; }
    public Skeleton OwnerSkeleton { get; private set; }
    public InventorySlot SelectedSlot => GetSlot(SelectedIndex);

    private Hand _subscribedSkeletonHand;
    private GameObject _heldItemVisual;

    private void Awake() {
        EnsureSlotListSize();
        RefreshHeldItemVisual();
    }

    private void OnDestroy() {
        ClearHeldItemVisual();

        if (_subscribedSkeletonHand != null) {
            _subscribedSkeletonHand.OnHandChanged -= HandleSkeletonHandChanged;
            _subscribedSkeletonHand = null;
        }
    }

    private void OnValidate() {
        maxSlots = Mathf.Max(1, maxSlots);
        EnsureSlotListSize();
        SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Mathf.Max(0, maxSlots - 1));
    }

    public void SetOwnerSkeleton(Skeleton skeleton) {
        if (_subscribedSkeletonHand != null) {
            _subscribedSkeletonHand.OnHandChanged -= HandleSkeletonHandChanged;
        }

        OwnerSkeleton = skeleton;
        _subscribedSkeletonHand = skeleton != null ? skeleton.Hand : null;

        if (_subscribedSkeletonHand != null) {
            _subscribedSkeletonHand.OnHandChanged += HandleSkeletonHandChanged;
        }

        MirrorCardsFromSkeleton();
    }

    public bool TryAddPickup(Pickupable pickup) {
        if (pickup == null) {
            return false;
        }

        CardData pickupCardData = IsCardItem(pickup.itemData) || pickup.itemData == null ? pickup.cardData : null;
        return TryAddItem(pickup.itemData, pickup.quantity, pickup.CaptureDropVisual(), pickupCardData);
    }

    public bool TryAddItem(ItemData item, int quantity, PickupDropVisual visual, CardData cardData = null) {
        if ((item == null && cardData == null) || quantity <= 0) {
            return false;
        }

        bool isCard = cardData != null && (item == null || IsCardItem(item));
        if (isCard) {
            if (OwnerSkeleton == null || !CanFitCard()) {
                return false;
            }

            return OwnerSkeleton.Hand.AddCard(cardData);
        }

        if (item == null || IsCardItem(item)) {
            return false;
        }

        if (!CanFitGeneralItem(item, quantity)) {
            return false;
        }

        int remaining = quantity;
        if (item.isStackable) {
            foreach (InventorySlot slot in slots) {
                if (remaining <= 0) {
                    break;
                }

                if (slot == null || slot.IsEmpty || slot.itemData != item || slot.cardData != null) {
                    continue;
                }

                int available = GetStackLimit(item) - slot.quantity;
                if (available <= 0) {
                    continue;
                }

                int moved = Mathf.Min(available, remaining);
                slot.quantity += moved;
                if (slot.dropVisual == null && visual != null) {
                    slot.dropVisual = visual.Copy();
                }

                remaining -= moved;
            }
        }

        while (remaining > 0) {
            InventorySlot slot = GetFirstEmptySlot();
            if (slot == null) {
                return false;
            }

            int moved = item.isStackable ? Mathf.Min(GetStackLimit(item), remaining) : 1;
            slot.SetItem(item, moved, visual, null);
            remaining -= moved;
        }

        RaiseHandChanged();
        return true;
    }

    public bool TryMoveSlotToInventory(int slotIndex, Inventory inventory) {
        if (inventory == null) {
            return false;
        }

        InventorySlot slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) {
            return false;
        }

        ItemData item = slot.itemData;
        if (item == null || slot.cardData != null || IsCardItem(item)) {
            return false;
        }

        if (!inventory.TryAddItem(item, slot.quantity, slot.dropVisual?.Copy(), null)) {
            return false;
        }

        slot.Clear();
        RaiseHandChanged();
        return true;
    }

    public bool TryMoveSelectedToInventory(Inventory inventory) {
        if (inventory == null) {
            return false;
        }

        return TryMoveSlotToInventory(SelectedIndex, inventory);
    }

    public bool TryTakeSlot(int slotIndex, InventorySlot destination) {
        InventorySlot slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || destination == null) {
            return false;
        }

        destination.CopyFrom(slot);
        if (slot.cardData != null) {
            if (OwnerSkeleton == null || !OwnerSkeleton.Hand.RemoveCard(slot.cardData)) {
                destination.Clear();
                return false;
            }
        }
        else {
            slot.Clear();
            RaiseHandChanged();
        }

        return true;
    }

    public bool TryPlaceSlot(int slotIndex, InventorySlot source, out InventorySlot swappedSlot) {
        swappedSlot = null;
        InventorySlot target = GetSlot(slotIndex);
        if (target == null || source == null || source.IsEmpty) {
            return false;
        }

        if (source.cardData != null || IsCardItem(source.itemData)) {
            return false;
        }

        swappedSlot = new InventorySlot();
        swappedSlot.CopyFrom(target);
        target.CopyFrom(source);
        RaiseHandChanged();
        return true;
    }

    public bool TryDropSlot(int slotIndex) {
        return DropSlot(slotIndex);
    }

    public bool TryTakeFromInventory(Inventory inventory, int slotIndex, int quantity = int.MaxValue) {
        if (inventory == null) {
            return false;
        }

        InventorySlot inventorySlot = inventory.GetSlot(slotIndex);
        if (inventorySlot == null || inventorySlot.IsEmpty) {
            return false;
        }

        int movedQuantity = Mathf.Clamp(quantity, 1, inventorySlot.quantity);
        PickupDropVisual visual = inventorySlot.dropVisual?.Copy();
        if (!TryAddItem(inventorySlot.itemData, movedQuantity, visual, inventorySlot.cardData)) {
            return false;
        }

        inventorySlot.quantity -= movedQuantity;
        if (inventorySlot.quantity <= 0) {
            inventorySlot.Clear();
        }

        inventory.NotifyInventoryChanged();
        return true;
    }

    public bool DropSelected() {
        return DropSlot(SelectedIndex);
    }

    public bool DropSlot(int slotIndex) {
        InventorySlot slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) {
            return false;
        }

        ItemData item = slot.itemData != null ? slot.itemData : cardItemData;
        CardData cardData = slot.cardData;
        PickupDropVisual visual = slot.dropVisual?.Copy() ?? CreateDefaultDropVisual(item);
        int quantity = Mathf.Max(1, slot.quantity);

        if (cardData != null) {
            if (OwnerSkeleton == null || !OwnerSkeleton.Hand.RemoveCard(cardData)) {
                return false;
            }
        }
        else {
            slot.Clear();
            RaiseHandChanged();
        }

        Pickupable pickup = CreateDroppedPickup(item, quantity, visual, cardData);
        RefreshHeldItemVisual();
        return pickup != null;
    }

    public void SelectSlot(int slotIndex) {
        int clamped = Mathf.Clamp(slotIndex, 0, Mathf.Max(0, maxSlots - 1));
        if (SelectedIndex == clamped) {
            return;
        }

        SelectedIndex = clamped;
        RefreshHeldItemVisual();
        OnSelectionChanged?.Invoke(SelectedIndex);
    }

    public void SelectNext() {
        SelectSlot((SelectedIndex + 1) % Mathf.Max(1, maxSlots));
    }

    public void SelectPrevious() {
        int count = Mathf.Max(1, maxSlots);
        SelectSlot((SelectedIndex - 1 + count) % count);
    }

    public InventorySlot GetSlot(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= slots.Count) {
            return null;
        }

        return slots[slotIndex];
    }

    public void NotifyHandChanged() {
        RaiseHandChanged();
    }

    private void HandleSkeletonHandChanged(List<CardData> _) {
        MirrorCardsFromSkeleton();
    }

    private void MirrorCardsFromSkeleton() {
        EnsureSlotListSize();

        List<InventorySlot> generalItems = new List<InventorySlot>();
        foreach (InventorySlot slot in slots) {
            if (slot == null || slot.IsEmpty || slot.cardData != null) {
                continue;
            }

            InventorySlot copy = new InventorySlot();
            copy.CopyFrom(slot);
            generalItems.Add(copy);
        }

        for (int i = 0; i < slots.Count; i++) {
            slots[i].Clear();
        }

        int index = 0;
        foreach (InventorySlot itemSlot in generalItems) {
            if (index >= slots.Count) {
                break;
            }

            slots[index].CopyFrom(itemSlot);
            index++;
        }

        if (OwnerSkeleton != null) {
            foreach (CardData card in OwnerSkeleton.Hand.GetCards()) {
                if (card == null || index >= slots.Count) {
                    break;
                }

                slots[index].SetItem(GetCardItemData(), 1, CreateDefaultDropVisual(GetCardItemData()), card);
                index++;
            }
        }

        RaiseHandChanged();
    }

    private bool CanFitCard() {
        return CountOccupiedSlots() < maxSlots;
    }

    private bool CanFitGeneralItem(ItemData item, int quantity) {
        int remaining = quantity;

        if (item.isStackable) {
            foreach (InventorySlot slot in slots) {
                if (slot == null || slot.itemData != item || slot.cardData != null) {
                    continue;
                }

                remaining -= Mathf.Max(0, GetStackLimit(item) - slot.quantity);
                if (remaining <= 0) {
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots) {
            if (slot == null || !slot.IsEmpty) {
                continue;
            }

            remaining -= item.isStackable ? GetStackLimit(item) : 1;
            if (remaining <= 0) {
                return true;
            }
        }

        return false;
    }

    private int CountOccupiedSlots() {
        int count = 0;
        foreach (InventorySlot slot in slots) {
            if (slot != null && !slot.IsEmpty) {
                count++;
            }
        }

        return count;
    }

    private InventorySlot GetFirstEmptySlot() {
        foreach (InventorySlot slot in slots) {
            if (slot != null && slot.IsEmpty) {
                return slot;
            }
        }

        return null;
    }

    private ItemData GetCardItemData() {
        return cardItemData;
    }


    private static bool IsCardItem(ItemData item) {
        return item != null && item.category == ItemCategory.Card;
    }
    private int GetStackLimit(ItemData item) {
        if (item == null || !item.isStackable) {
            return 1;
        }

        return Mathf.Max(1, item.maxStackSize);
    }

    private Pickupable CreateDroppedPickup(ItemData item, int quantity, PickupDropVisual visual, CardData cardData) {
        Vector3 dropPosition = transform.position + transform.TransformDirection(dropOffset);
        Quaternion dropRotation = Quaternion.identity;
        GameObject droppedObject;
        GameObject prefab = visual != null && visual.prefab != null
            ? visual.prefab
            : item != null && item.dropPrefab != null
                ? item.dropPrefab
                : pickupPrefab;

        if (prefab != null) {
            droppedObject = Instantiate(prefab, dropPosition, dropRotation);
        }
        else {
            droppedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droppedObject.transform.position = dropPosition;
        }

        if (visual != null) {
            droppedObject.transform.localScale = visual.scale;
        }

        Pickupable pickup = droppedObject.GetComponent<Pickupable>();
        if (pickup == null) {
            pickup = droppedObject.AddComponent<Pickupable>();
        }

        pickup.itemData = item;
        pickup.cardData = cardData;
        pickup.quantity = quantity;
        pickup.focusTint = visual != null ? visual.focusTint : new Color(1f, 0.9f, 0.35f, 1f);
        pickup.tintRenderersOnFocus = visual == null || visual.tintRenderersOnFocus;
        pickup.SetPickupable(true);
        ApplyCardVisual(droppedObject, cardData);

        Rigidbody body = droppedObject.GetComponent<Rigidbody>();
        if (body == null) {
            body = droppedObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        EnsureSolidDropCollider(droppedObject);

        if (giveDroppedItemForwardVelocity) {
            body.linearVelocity = transform.forward * droppedItemForwardVelocity;
        }

        return pickup;
    }

    private PickupDropVisual CreateDefaultDropVisual(ItemData item) {
        if (item != null && item.dropPrefab != null) {
            Pickupable prefabPickup = item.dropPrefab.GetComponent<Pickupable>();
            Color focusTint = prefabPickup != null ? prefabPickup.focusTint : new Color(1f, 0.9f, 0.35f, 1f);
            bool tintOnFocus = prefabPickup == null || prefabPickup.tintRenderersOnFocus;
            return new PickupDropVisual(item.dropPrefab, item.dropPrefab.transform.localScale, focusTint, tintOnFocus);
        }

        return new PickupDropVisual(null, Vector3.one, new Color(1f, 0.9f, 0.35f, 1f), true);
    }


    private void RefreshHeldItemVisual() {
        ClearHeldItemVisual();

        if (!showSelectedItemInHand) {
            return;
        }

        InventorySlot slot = SelectedSlot;
        if (slot == null || slot.IsEmpty) {
            return;
        }

        Transform anchor = GetHeldItemAnchor();
        ItemData item = slot.itemData != null ? slot.itemData : cardItemData;
        GameObject prefab = slot.dropVisual != null && slot.dropVisual.prefab != null
            ? slot.dropVisual.prefab
            : item != null ? item.dropPrefab : null;

        _heldItemVisual = prefab != null
            ? Instantiate(prefab, anchor)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        _heldItemVisual.name = item != null && !string.IsNullOrWhiteSpace(item.itemName)
            ? $"Held {item.itemName}"
            : "Held Item";

        if (_heldItemVisual.transform.parent != anchor) {
            _heldItemVisual.transform.SetParent(anchor, false);
        }

        _heldItemVisual.transform.localPosition = Vector3.zero;
        _heldItemVisual.transform.localRotation = Quaternion.identity;
        _heldItemVisual.transform.localScale = heldItemLocalScale;
        DisableHeldPhysics(_heldItemVisual);
        ApplyCardVisual(_heldItemVisual, slot.cardData);
    }
    private Transform GetHeldItemAnchor() {
        if (heldItemAnchor != null) {
            return heldItemAnchor;
        }

        Transform existing = transform.Find("HeldItemAnchor");
        if (existing != null) {
            heldItemAnchor = existing;
            return heldItemAnchor;
        }

        GameObject anchorObject = new GameObject("HeldItemAnchor");
        heldItemAnchor = anchorObject.transform;
        heldItemAnchor.SetParent(transform, false);
        heldItemAnchor.localPosition = heldItemLocalPosition;
        heldItemAnchor.localRotation = Quaternion.Euler(heldItemLocalEulerAngles);
        heldItemAnchor.localScale = Vector3.one;
        return heldItemAnchor;
    }

    private void ClearHeldItemVisual() {
        if (_heldItemVisual == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(_heldItemVisual);
        }
        else {
            DestroyImmediate(_heldItemVisual);
        }

        _heldItemVisual = null;
    }

    private static void DisableHeldPhysics(GameObject heldObject) {
        if (heldObject == null) {
            return;
        }

        Pickupable[] pickups = heldObject.GetComponentsInChildren<Pickupable>();
        for (int i = 0; i < pickups.Length; i++) {
            pickups[i].SetPickupable(false);
            pickups[i].enabled = false;
        }

        Rigidbody[] bodies = heldObject.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < bodies.Length; i++) {
            bodies[i].isKinematic = true;
            bodies[i].useGravity = false;
            bodies[i].linearVelocity = Vector3.zero;
            bodies[i].angularVelocity = Vector3.zero;
        }

        Collider[] colliders = heldObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++) {
            colliders[i].enabled = false;
        }
    }

    private static void ApplyCardVisual(GameObject target, CardData cardData) {
        if (target == null || cardData == null) {
            return;
        }

        WorldCardView cardView = target.GetComponentInChildren<WorldCardView>();
        if (cardView != null) {
            cardView.SetCard(cardData);
        }
    }

    private static void EnsureSolidDropCollider(GameObject droppedObject) {
        if (droppedObject == null) {
            return;
        }

        Collider[] colliders = droppedObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++) {
            if (colliders[i] != null && !colliders[i].isTrigger) {
                return;
            }
        }

        Bounds bounds = CalculateRendererBounds(droppedObject);
        BoxCollider solidCollider = droppedObject.AddComponent<BoxCollider>();
        solidCollider.isTrigger = false;

        if (bounds.size.sqrMagnitude <= 0.0001f) {
            solidCollider.size = Vector3.one;
            solidCollider.center = Vector3.zero;
            return;
        }

        solidCollider.center = droppedObject.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = droppedObject.transform.InverseTransformVector(bounds.size);
        solidCollider.size = new Vector3(
            Mathf.Max(0.05f, Mathf.Abs(localSize.x)),
            Mathf.Max(0.05f, Mathf.Abs(localSize.y)),
            Mathf.Max(0.05f, Mathf.Abs(localSize.z)));
    }

    private static Bounds CalculateRendererBounds(GameObject target) {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) {
            return new Bounds(target.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) {
            if (renderers[i] != null) {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return bounds;
    }
    private void EnsureSlotListSize() {
        if (slots == null) {
            slots = new List<InventorySlot>();
        }

        while (slots.Count < maxSlots) {
            slots.Add(new InventorySlot());
        }

        if (slots.Count > maxSlots) {
            slots.RemoveRange(maxSlots, slots.Count - maxSlots);
        }

        for (int i = 0; i < slots.Count; i++) {
            if (slots[i] == null) {
                slots[i] = new InventorySlot();
            }

            slots[i].Normalize();
        }
    }

    private void RaiseHandChanged() {
        SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Mathf.Max(0, maxSlots - 1));
        RefreshHeldItemVisual();
        OnHandChanged?.Invoke();
        OnSelectionChanged?.Invoke(SelectedIndex);
    }
}








