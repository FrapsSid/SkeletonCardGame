using UnityEngine;

public class SimpleItem : IItem
{
    private readonly GameObject _prefab;
    public string Name { get; }
    public string Description { get; }

    public SimpleItem(string name, string description, GameObject prefab)
    {
        Name = name;
        Description = description;
        _prefab = prefab;
    }

    public GameObject CreateHeldObject()
    {
        return ItemUtils.InstantiateWithPickupable(_prefab, false);
    }

    public GameObject CreateInventoryView()
    {
        return ItemUtils.InstantiateWithPickupable(_prefab, false);
    }

    public GameObject CreateDropped()
    {
        return ItemUtils.InstantiateWithPickupable(_prefab, true);
    }
}