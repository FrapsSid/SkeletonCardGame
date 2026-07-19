using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardsItem : IItem
{
    public readonly IReadOnlyList<CardData> Cards;
    private GameObject _cardStackPrefab;
    private Skeleton? _owner;

    public CardsItem(GameObject cardStackPrefab, IList<CardData> cards, Skeleton? owner = null)
    {
        _cardStackPrefab = cardStackPrefab;
        _owner = owner;
        Cards = cards.ToList().AsReadOnly();
    }

    private GameObject Create()
    {
        var obj = Object.Instantiate(_cardStackPrefab);
        var stack = obj.GetComponent<CardStack>();
        if (stack != null)
            stack.SetOwner(_owner);
        stack.SetCards(Cards);
        return obj;
    }

    public string Name => "Cards";
    public string Description => "Cards";
    public bool CanBePutInInventory => false;

    public GameObject CreateHeldObject()
    {
        return Create();
    }

    public GameObject CreateInventoryView()
    {
        return Create();
    }

    public GameObject CreateDropped()
    {
        var obj = Create();
        Pickupable pickupable = obj.AddComponent<Pickupable>();
        pickupable.Item = this;
        return obj;
    }
}
