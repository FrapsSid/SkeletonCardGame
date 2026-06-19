using System;
using UnityEngine;

[Serializable]
public class PickupDropVisual {
    public GameObject prefab;
    public Vector3 scale = Vector3.one;
    public Color focusTint = new Color(1f, 0.9f, 0.35f, 1f);
    public bool tintRenderersOnFocus = true;

    public PickupDropVisual() {
    }

    public PickupDropVisual(GameObject prefab, Vector3 scale, Color focusTint, bool tintRenderersOnFocus) {
        this.prefab = prefab;
        this.scale = scale;
        this.focusTint = focusTint;
        this.tintRenderersOnFocus = tintRenderersOnFocus;
    }

    public PickupDropVisual Copy() {
        return new PickupDropVisual(prefab, scale, focusTint, tintRenderersOnFocus);
    }
}

[Serializable]
public class InventorySlot {
    public ItemData itemData;
    public int quantity;
    public PickupDropVisual dropVisual;

    public bool IsEmpty => itemData == null;

    public void Clear() {
        itemData = null;
        quantity = 0;
        dropVisual = null;
    }

    public void SetItem(ItemData item, int qty, PickupDropVisual visual = null) {
        itemData = item;
        quantity = item == null ? 0 : Math.Max(0, qty);
        dropVisual = visual?.Copy();

        if (quantity <= 0) {
            Clear();
        }
    }

    public void CopyFrom(InventorySlot source) {
        if (source == null || source.IsEmpty) {
            Clear();
            return;
        }

        SetItem(source.itemData, source.quantity, source.dropVisual);
    }
}