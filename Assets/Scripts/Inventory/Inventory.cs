#nullable enable
using UnityEngine;

[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    [SerializeField] private int maxSlots = 9;
    public int MaxSlots => maxSlots;
    public IItem?[] Items { get; private set; } = null!;

    private void Awake()
    {
        Items = new IItem?[maxSlots];
    }

    public void SetMaxSlots(int slots)
    {
        slots = Mathf.Max(1, slots);
        EnsureItems();

        if (slots < Items.Length)
        {
            for (int i = slots; i < Items.Length; i++)
            {
                IItem? item = Items[i];
                if (item != null)
                {
                    ItemUtils.DropItem(item, transform.position, transform.rotation);
                }
            }
        }

        IItem?[] resizedItems = new IItem?[slots];
        int copiedCount = Mathf.Min(Items.Length, resizedItems.Length);
        for (int i = 0; i < copiedCount; i++)
        {
            resizedItems[i] = Items[i];
        }

        maxSlots = slots;
        Items = resizedItems;
    }

    public void SwapWithHand(PlayerHand hand, int slot)
    {
        if (hand == null || slot < 0 || slot >= maxSlots)
        {
            return;
        }

        EnsureItems();
        IItem? inventoryItem = Items[slot];
        Items[slot] = hand.Item;
        hand.SetItem(inventoryItem);
    }

    public bool TryAdd(IItem item)
    {
        EnsureItems();

        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i] != null)
            {
                continue;
            }

            Items[i] = item;
            return true;
        }

        return false;
    }

    private void EnsureItems()
    {
        if (Items != null && Items.Length == maxSlots)
        {
            return;
        }

        IItem?[] resizedItems = new IItem?[Mathf.Max(1, maxSlots)];
        if (Items != null)
        {
            int copiedCount = Mathf.Min(Items.Length, resizedItems.Length);
            for (int i = 0; i < copiedCount; i++)
            {
                resizedItems[i] = Items[i];
            }
        }

        Items = resizedItems;
    }
}
