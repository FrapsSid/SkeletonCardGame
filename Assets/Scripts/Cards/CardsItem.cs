using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardsItem : IItem
{
    public readonly IReadOnlyList<CardData> Cards;
    private GameObject _cardStackPrefab;

    public CardsItem(GameObject cardStackPrefab, IList<CardData> cards)
    {
        _cardStackPrefab = cardStackPrefab;
        Cards = cards.ToList().AsReadOnly();
    }

    private GameObject Create()
    {
        var obj = Object.Instantiate(_cardStackPrefab);
        obj.GetComponent<CardStack>().SetCards(Cards);
        return obj;
    }

    public string Name => "Cards";
    public string Description => "Cards";

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
