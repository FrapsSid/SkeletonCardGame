using UnityEngine;

public interface IItem
{
    string Name { get; }
    string Description { get; }
    bool CanBePutInInventory { get; }
    GameObject CreateHeldObject();
    GameObject CreateInventoryView();
    GameObject CreateDropped();
}
