#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BodyPartItem : IItem
{
    private readonly GameObject _prefab;
    public readonly BodyPartType Type;
    public readonly Skeleton? OriginalOwner;

    public BodyPartItem(GameObject prefab, BodyPartType type, Skeleton? originalOwner)
    {
        _prefab = prefab;
        Type = type;
        OriginalOwner = originalOwner;
    }

    public string Name => Type.ToString();
    public string Description => $"{OriginalOwner}'s {Type}";
    public bool CanBePutInInventory => Type != BodyPartType.Head;

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
        GameObject droppedObject = Object.Instantiate(_prefab);
        BodyPart bodyPart = droppedObject.GetComponent<BodyPart>() ?? throw new Exception("invalid prefab without BodyPart");
        bodyPart.Initialize(this);
        bodyPart.Detach();
        return droppedObject;
    }
}
