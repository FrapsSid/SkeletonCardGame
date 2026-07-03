using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class PlayerInventoryOwner : MonoBehaviour {
    public Inventory Inventory { get; private set; }
    public PlayerHand leftHand;
    public PlayerHand rightHand;
    
    public Skeleton OwnerSkeleton { get; private set; }

    private void Awake() {
        Inventory = GetComponent<Inventory>();
    }

    public void AssignSkeleton(Skeleton skeleton) {
        OwnerSkeleton = skeleton;
        OwnerSkeleton.SetInventoryOwner(this);
    }
}
