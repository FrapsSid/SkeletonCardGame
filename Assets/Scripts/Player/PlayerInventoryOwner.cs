using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class PlayerInventoryOwner : MonoBehaviour {
    public Inventory Inventory { get; private set; }
    public PlayerController Controller { get; private set; }

    private void Awake() {
        Inventory = GetComponent<Inventory>();
        Controller = GetComponent<PlayerController>();
    }
}