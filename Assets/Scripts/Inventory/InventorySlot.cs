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
    public CardData cardData;
    public int quantity;
    public PickupDropVisual dropVisual;

    public bool IsEmpty => itemData == null && cardData == null;
    public bool HasCard => cardData != null;

    public void Clear() {
        itemData = null;
        cardData = null;
        quantity = 0;
        dropVisual = null;
    }

    public void SetItem(ItemData item, int qty, PickupDropVisual visual = null, CardData card = null) {
        itemData = item;
        cardData = card;
        quantity = Math.Max(0, qty);
        dropVisual = visual?.Copy();
        Normalize();
    }

    public void CopyFrom(InventorySlot source) {
        if (source == null || source.IsEmpty) {
            Clear();
            return;
        }

        SetItem(source.itemData, source.quantity, source.dropVisual, source.cardData);
    }

    public void Normalize() {
        quantity = Math.Max(0, quantity);

        if (itemData != null && itemData.category != ItemCategory.Card) {
            cardData = null;
        }

        if (quantity == 0 || (itemData == null && cardData == null)) {
            Clear();
        }
    }
}
