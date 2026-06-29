using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Inventory : MonoBehaviour {
    [Header("Capacity")] [Min(1)] public int maxSlots = 9;
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Dropping")] public GameObject pickupPrefab;
    public Vector3 dropOffset = new Vector3(0f, 0.5f, 1.25f);
    public bool giveDroppedItemForwardVelocity = true;
    public float droppedItemForwardVelocity = 1.5f;

    public int CurrentItemCount {
        get {
            int count = 0;
            foreach (InventorySlot slot in slots) {
                if (slot != null && !slot.IsEmpty) {
                    count++;
                }
            }

            return count;
        }
    }

    public bool IsFull => CurrentItemCount >= maxSlots;

    public event Action<ItemData, int> OnItemAdded;
    public event Action<ItemData, int> OnItemRemoved;
    public event Action<ItemData, int> OnItemDropped;
    public event Action OnInventoryChanged;

    private void Awake() {
        EnsureSlotListSize();
    }

    private void OnValidate() {
        maxSlots = Mathf.Max(1, maxSlots);
        EnsureSlotListSize();
    }

    public bool TryAddPickup(Pickupable pickup) {
        if (pickup == null) {
            return false;
        }

        CardData pickupCardData = pickup.itemData != null && pickup.itemData.category == ItemCategory.Card ? pickup.cardData : null;
        return TryAddItem(pickup.itemData, pickup.quantity, pickup.CaptureDropVisual(), pickupCardData);
    }

    public bool TryAddItem(ItemData item, int quantity = 1) {
        return TryAddItem(item, quantity, CreateDefaultDropVisual(item), null);
    }

    public bool TryAddItem(ItemData item, int quantity, CardData cardData) {
        return TryAddItem(item, quantity, CreateDefaultDropVisual(item), cardData);
    }

    public bool TryAddItem(ItemData item, int quantity, PickupDropVisual visual, CardData cardData = null) {
        bool isCardItem = item != null && item.category == ItemCategory.Card;
        if (item == null
            || quantity <= 0
            || (isCardItem && cardData == null)
            || (!isCardItem && cardData != null)
            || !CanFitItem(item, quantity, cardData)) {
            return false;
        }

        int remaining = quantity;
        bool canStack = item.isStackable && cardData == null;

        if (canStack) {
            foreach (InventorySlot slot in slots) {
                if (remaining <= 0) {
                    break;
                }

                if (slot == null || slot.itemData != item || slot.cardData != null) {
                    continue;
                }

                int stackLimit = GetStackLimit(item);
                int available = stackLimit - slot.quantity;
                if (available <= 0) {
                    continue;
                }

                int toAdd = Mathf.Min(available, remaining);
                slot.quantity += toAdd;
                if (slot.dropVisual == null && visual != null) {
                    slot.dropVisual = visual.Copy();
                }

                remaining -= toAdd;
            }
        }

        while (remaining > 0) {
            InventorySlot emptySlot = GetFirstEmptySlot();
            if (emptySlot == null) {
                return false;
            }

            int toAdd = canStack ? Mathf.Min(GetStackLimit(item), remaining) : 1;
            emptySlot.SetItem(item, toAdd, visual, cardData);
            remaining -= toAdd;
        }

        OnItemAdded?.Invoke(item, quantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item, int quantity = 1) {
        if (item == null || quantity <= 0 || GetItemCount(item) < quantity) {
            return false;
        }

        int remaining = quantity;

        foreach (InventorySlot slot in slots) {
            if (remaining <= 0) {
                break;
            }

            if (slot == null || slot.itemData != item) {
                continue;
            }

            int toRemove = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= toRemove;
            remaining -= toRemove;

            if (slot.quantity <= 0) {
                slot.Clear();
            }
        }

        OnItemRemoved?.Invoke(item, quantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(ItemData item, int quantity = 1) {
        return item != null && quantity > 0 && GetItemCount(item) >= quantity;
    }

    public List<(ItemData itemData, int quantity)> GetAllItems() {
        List<(ItemData itemData, int quantity)> items = new List<(ItemData itemData, int quantity)>();

        foreach (InventorySlot slot in slots) {
            if (slot != null && !slot.IsEmpty && slot.quantity > 0) {
                items.Add((slot.itemData, slot.quantity));
            }
        }

        return items;
    }

    public int GetItemCount(ItemData item) {
        if (item == null) {
            return 0;
        }

        int count = 0;

        foreach (InventorySlot slot in slots) {
            if (slot != null && slot.itemData == item) {
                count += Mathf.Max(0, slot.quantity);
            }
        }

        return count;
    }

    public InventorySlot GetFirstEmptySlot() {
        foreach (InventorySlot slot in slots) {
            if (slot != null && slot.IsEmpty) {
                return slot;
            }
        }

        return null;
    }

    public InventorySlot GetSlot(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= slots.Count) {
            return null;
        }

        return slots[slotIndex];
    }

    public int GetStackLimitFor(ItemData item) {
        return GetStackLimit(item);
    }

    public void NotifyInventoryChanged() {
        OnInventoryChanged?.Invoke();
    }

    public bool DropItem(int slotIndex, int quantity = 1) {
        InventorySlot slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) {
            return false;
        }

        ItemData item = slot.itemData;
        CardData cardData = slot.cardData;
        PickupDropVisual visual = slot.dropVisual?.Copy() ?? CreateDefaultDropVisual(item);
        int dropQuantity = Mathf.Clamp(quantity, 1, slot.quantity);

        Pickupable pickup = CreateDroppedPickup(item, dropQuantity, visual, cardData);
        if (pickup == null) {
            return false;
        }

        slot.quantity -= dropQuantity;
        if (slot.quantity <= 0) {
            slot.Clear();
        }

        OnItemDropped?.Invoke(item, dropQuantity);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool DropLooseItem(ItemData item, int quantity, PickupDropVisual visual, CardData cardData = null) {
        if (item == null || quantity <= 0) {
            return false;
        }

        Pickupable pickup = CreateDroppedPickup(item, quantity, visual ?? CreateDefaultDropVisual(item), cardData);
        if (pickup == null) {
            return false;
        }

        OnItemDropped?.Invoke(item, quantity);
        return true;
    }

    public bool MoveItem(int fromSlotIndex, int toSlotIndex) {
        if (fromSlotIndex == toSlotIndex) {
            return false;
        }

        InventorySlot fromSlot = GetSlot(fromSlotIndex);
        InventorySlot toSlot = GetSlot(toSlotIndex);
        if (fromSlot == null || toSlot == null || fromSlot.IsEmpty) {
            return false;
        }

        if (toSlot.IsEmpty) {
            toSlot.CopyFrom(fromSlot);
            fromSlot.Clear();
            OnInventoryChanged?.Invoke();
            return true;
        }

        if (CanMergeSlots(fromSlot, toSlot)) {
            int available = GetStackLimit(toSlot.itemData) - toSlot.quantity;
            int moved = Mathf.Min(available, fromSlot.quantity);
            if (moved <= 0) {
                return false;
            }

            toSlot.quantity += moved;
            fromSlot.quantity -= moved;
            if (fromSlot.quantity <= 0) {
                fromSlot.Clear();
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        InventorySlot tempSlot = new InventorySlot();
        tempSlot.CopyFrom(toSlot);
        toSlot.CopyFrom(fromSlot);
        fromSlot.CopyFrom(tempSlot);

        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool CanFitItem(ItemData item, int quantity, CardData cardData) {
        if (item == null) {
            return false;
        }

        int remaining = quantity;
        bool canStack = item.isStackable && cardData == null;

        if (canStack) {
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

            remaining -= canStack ? GetStackLimit(item) : 1;
            if (remaining <= 0) {
                return true;
            }
        }

        return false;
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
            droppedObject.name = item != null ? $"Dropped {item.itemName}" : "Dropped Item";
            droppedObject.transform.position = dropPosition;
        }

        if (visual != null) {
            droppedObject.transform.localScale = visual.scale;
        }

        string itemName = item != null && !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : "Dropped Item";
        droppedObject.name = quantity > 1 ? $"{itemName} x{quantity}" : itemName;

        Pickupable pickup = droppedObject.GetComponent<Pickupable>();
        if (pickup == null) {
            pickup = droppedObject.AddComponent<Pickupable>();
        }

        pickup.itemData = item;
        pickup.cardData = cardData;
        pickup.quantity = quantity;
        if (visual != null) {
            pickup.focusTint = visual.focusTint;
            pickup.tintRenderersOnFocus = visual.tintRenderersOnFocus;
        }

        pickup.SetPickupable(true);

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

    private bool CanMergeSlots(InventorySlot fromSlot, InventorySlot toSlot) {
        return fromSlot.itemData != null
               && fromSlot.cardData == null
               && toSlot.cardData == null
               && fromSlot.itemData == toSlot.itemData
               && fromSlot.itemData.isStackable
               && toSlot.quantity < GetStackLimit(toSlot.itemData);
    }

    private int GetStackLimit(ItemData item) {
        if (item == null || !item.isStackable) {
            return 1;
        }

        return Mathf.Max(1, item.maxStackSize);
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
}
