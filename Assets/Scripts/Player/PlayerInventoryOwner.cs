using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class PlayerInventoryOwner : MonoBehaviour {
    public Inventory Inventory { get; private set; }
    [SerializeField] private PlayerHand leftHand;
    [SerializeField] private PlayerHand rightHand;
    
    public PlayerHand LeftHand => leftHand;
    public PlayerHand RightHand => rightHand;
    public Skeleton OwnerSkeleton { get; private set; }

    private void Awake() {
        Inventory = GetComponent<Inventory>();
    }

    public void AssignSkeleton(Skeleton skeleton) {
        OwnerSkeleton = skeleton;
        OwnerSkeleton.SetInventoryOwner(this);
    }
}
