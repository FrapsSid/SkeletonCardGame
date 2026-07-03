#nullable enable

using System.Collections.Generic;
using Interactions;
using UnityEngine;

[DisallowMultipleComponent]
public class Pickupable : MonoBehaviour, IInteractable
{
    public const string InteractionText = "Pickup";

    [Header("Item")] 
    public IItem? Item;
    [Min(0f)] public float interactionRadius = 2f;
    public bool addTriggerColliderIfMissing = true;

    private void Reset()
    {
        EnsureDiscoveryCollider();
    }

    private void Awake()
    {
        EnsureDiscoveryCollider();
    }

    private void OnValidate()
    {
        interactionRadius = Mathf.Max(0f, interactionRadius);
    }

    public void Pickup(PlayerInventoryOwner player, InteractionType interactionType)
    {
        if (player == null || Item == null)
        {
            return;
        }

        bool pickedUp = interactionType switch
        {
            InteractionType.LeftHand => TrySetHand(player.leftHand),
            InteractionType.RightHand => TrySetHand(player.rightHand),
            _ => TrySetHand(player.leftHand) || TrySetHand(player.rightHand) || player.Inventory.TryAdd(Item)
        };

        if (pickedUp)
        {
            Destroy(gameObject);
        }
    }

    public IList<Interaction> GetInteractions(Skeleton player)
    {
        if (!CanPickup(player))
        {
            return new List<Interaction>();
        }

        return new List<Interaction>
        {
            new(Item!.Name, InteractionText, type => Pickup(player.InventoryOwner, type))
        };
    }

    private void EnsureDiscoveryCollider()
    {
        if (!addTriggerColliderIfMissing || GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        SphereCollider discoveryCollider = gameObject.AddComponent<SphereCollider>();
        discoveryCollider.isTrigger = true;
        discoveryCollider.radius = Mathf.Max(0.1f, interactionRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    private bool CanPickup(Skeleton player)
    {
        if (player?.InventoryOwner == null || Item == null || !IsPlayerInRange(player.InventoryOwner))
        {
            return false;
        }

        PlayerInventoryOwner owner = player.InventoryOwner;
        return IsFree(owner.leftHand)
            || IsFree(owner.rightHand)
            || (Item.CanBePutInInventory && HasFreeInventorySlot(owner.Inventory));
    }

    private bool IsPlayerInRange(PlayerInventoryOwner player)
    {
        return player != null && Vector3.Distance(transform.position, player.transform.position) <= interactionRadius;
    }

    private bool TrySetHand(PlayerHand hand)
    {
        if (!IsFree(hand) || Item == null)
        {
            return false;
        }

        hand.SetItem(Item);
        return true;
    }

    private static bool IsFree(PlayerHand hand)
    {
        return hand != null && !hand.HasItem;
    }

    private static bool HasFreeInventorySlot(Inventory inventory)
    {
        if (inventory == null)
        {
            return false;
        }

        IItem?[] items = inventory.Items;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                return true;
            }
        }

        return false;
    }
}
