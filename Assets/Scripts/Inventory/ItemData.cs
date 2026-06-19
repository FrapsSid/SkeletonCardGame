using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Skeleton Card Game/Item")]
public class ItemData : ScriptableObject {
    [Header("Identity")] public string itemId;
    public string itemName;
    [TextArea] public string description;

    [Header("Presentation")] public Sprite icon;
    public GameObject dropPrefab;
    public ItemCategory category = ItemCategory.General;

    [Header("Stacking")] public bool isStackable;
    [Min(1)] public int maxStackSize = 1;

    private void OnValidate() {
        maxStackSize = Mathf.Max(1, maxStackSize);

        if (!isStackable) {
            maxStackSize = 1;
        }
    }
}