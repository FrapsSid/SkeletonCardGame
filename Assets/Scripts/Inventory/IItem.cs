using UnityEngine;

public interface IItem
{
    string Name { get; }
    string Description { get; }
    GameObject CreateHeldObject();
    GameObject CreateInventoryView();
    GameObject CreateDropped();
}