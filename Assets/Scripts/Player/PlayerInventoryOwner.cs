using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class PlayerInventoryOwner : MonoBehaviour {
    public Inventory Inventory { get; private set; }
    public PlayerHand Hand { get; private set; }
    public PlayerController Controller { get; private set; }
    public Skeleton OwnerSkeleton { get; private set; }

    private void Awake() {
        Inventory = GetComponent<Inventory>();
        Hand = GetComponent<PlayerHand>();
        Controller = GetComponent<PlayerController>();
    }

    public void AssignSkeleton(Skeleton skeleton) {
        OwnerSkeleton = skeleton;
        if (Hand != null) {
            Hand.SetOwnerSkeleton(skeleton);
        }
    }

    public bool TryPickup(Pickupable pickup) {
        if (pickup == null) {
            return false;
        }

        if (Hand != null && Hand.TryAddPickup(pickup)) {
            return true;
        }

        return Inventory != null && Inventory.TryAddPickup(pickup);
    }
}
