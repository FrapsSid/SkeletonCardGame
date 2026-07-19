#nullable enable

using System.Collections.Generic;
using Interactions;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerTableCardStackInteractable : MonoBehaviour, IInteractable
{
    private const string Source = "Cards";
    private const string TakeText = "Take cards";
    private const string PlaceText = "Place cards";

    [SerializeField] private PlayerTableCardStacks? playerTableCardStacks;
    [SerializeField, Min(0f)] private float interactionRadius = 0.6f;
    [SerializeField] private bool addTriggerColliderIfMissing = true;

    private Skeleton? owner;

    public Skeleton? Owner => owner;

    private void Reset()
    {
        playerTableCardStacks = FindFirstObjectByType<PlayerTableCardStacks>();
        EnsureDiscoveryCollider();
    }

    private void Awake()
    {
        EnsureDiscoveryCollider();
    }

    private void OnValidate()
    {
        interactionRadius = Mathf.Max(0f, interactionRadius);
        EnsureDiscoveryCollider();
    }

    public void Initialize(PlayerTableCardStacks cardStacks, Skeleton player)
    {
        playerTableCardStacks = cardStacks;
        owner = player;
        EnsureDiscoveryCollider();
    }

    public void ClearOwner()
    {
        owner = null;
    }

    public IList<Interaction> GetInteractions(Skeleton player)
    {
        if (!IsOwner(player) || player.InventoryOwner == null || playerTableCardStacks == null)
        {
            return new List<Interaction>();
        }

        bool canTake = CanTake(player.InventoryOwner);
        bool canPlace = CanPlace(player.InventoryOwner);
        if (!canTake && !canPlace)
        {
            return new List<Interaction>();
        }

        var interactions = new List<Interaction>(2);
        if (canTake)
        {
            interactions.Add(new Interaction(Source, TakeText, type => TakeCards(player.InventoryOwner, type)));
        }

        if (canPlace)
        {
            interactions.Add(new Interaction(Source, PlaceText, type => PlaceCards(player.InventoryOwner, type), !canTake));
        }

        return interactions;
    }

    private bool CanTake(PlayerInventoryOwner inventoryOwner)
    {
        return owner != null
            && playerTableCardStacks != null
            && playerTableCardStacks.CardStackPrefab != null
            && playerTableCardStacks.HasCards(owner)
            && TryGetTakeTargetHand(inventoryOwner, InteractionType.Other, out _);
    }

    private static bool CanPlace(PlayerInventoryOwner inventoryOwner)
    {
        return TryGetCardsHand(inventoryOwner, InteractionType.Other, out _, out _);
    }

    private void TakeCards(PlayerInventoryOwner inventoryOwner, InteractionType interactionType)
    {
        if (owner == null
            || playerTableCardStacks == null
            || playerTableCardStacks.CardStackPrefab == null
            || !TryGetTakeTargetHand(inventoryOwner, interactionType, out PlayerHand hand)
            || !playerTableCardStacks.TryTakeCards(owner, out List<CardData> tableCards))
        {
            return;
        }

        var cards = new List<CardData>();
        if (hand.Item is CardsItem heldCards)
        {
            cards.AddRange(heldCards.Cards);
        }

        cards.AddRange(tableCards);
        hand.SetItem(new CardsItem(playerTableCardStacks.CardStackPrefab.gameObject, cards, owner));
    }

    private void PlaceCards(PlayerInventoryOwner inventoryOwner, InteractionType interactionType)
    {
        if (owner == null
            || playerTableCardStacks == null
            || !TryGetCardsHand(inventoryOwner, interactionType, out PlayerHand hand, out CardsItem cardsItem))
        {
            return;
        }

        playerTableCardStacks.PlaceStack(owner, cardsItem.Cards);
        hand.SetItem(null);
    }

    private bool IsOwner(Skeleton player)
    {
        return owner != null && ReferenceEquals(player, owner);
    }

    private void EnsureDiscoveryCollider()
    {
        if (!addTriggerColliderIfMissing)
        {
            return;
        }

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
            sphereCollider.radius = Mathf.Max(0.1f, interactionRadius);
            return;
        }

        if (GetComponent<Collider>() != null)
        {
            return;
        }

        sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = Mathf.Max(0.1f, interactionRadius);
    }

    private static bool TryGetTakeTargetHand(PlayerInventoryOwner inventoryOwner, InteractionType interactionType, out PlayerHand hand)
    {
        hand = null!;
        return interactionType switch
        {
            InteractionType.LeftHand => TryGetCompatibleHand(inventoryOwner.leftHand, out hand),
            InteractionType.RightHand => TryGetCompatibleHand(inventoryOwner.rightHand, out hand),
            _ => TryGetCardsHand(inventoryOwner, InteractionType.Other, out hand, out _)
                || TryGetFreeHand(inventoryOwner.leftHand, out hand)
                || TryGetFreeHand(inventoryOwner.rightHand, out hand)
        };
    }

    private static bool TryGetCardsHand(
        PlayerInventoryOwner inventoryOwner,
        InteractionType interactionType,
        out PlayerHand hand,
        out CardsItem cardsItem)
    {
        hand = null!;
        cardsItem = null!;
        return interactionType switch
        {
            InteractionType.LeftHand => TryGetCardsHand(inventoryOwner.leftHand, out hand, out cardsItem),
            InteractionType.RightHand => TryGetCardsHand(inventoryOwner.rightHand, out hand, out cardsItem),
            _ => TryGetCardsHand(inventoryOwner.leftHand, out hand, out cardsItem)
                || TryGetCardsHand(inventoryOwner.rightHand, out hand, out cardsItem)
        };
    }

    private static bool TryGetCompatibleHand(PlayerHand hand, out PlayerHand compatibleHand)
    {
        if (TryGetCardsHand(hand, out compatibleHand, out _))
        {
            return true;
        }

        return TryGetFreeHand(hand, out compatibleHand);
    }

    private static bool TryGetCardsHand(PlayerHand hand, out PlayerHand cardsHand, out CardsItem cardsItem)
    {
        if (hand != null && hand.Item is CardsItem item && item.Cards.Count > 0)
        {
            cardsHand = hand;
            cardsItem = item;
            return true;
        }

        cardsHand = null!;
        cardsItem = null!;
        return false;
    }

    private static bool TryGetFreeHand(PlayerHand hand, out PlayerHand freeHand)
    {
        if (hand != null && !hand.HasItem)
        {
            freeHand = hand;
            return true;
        }

        freeHand = null!;
        return false;
    }
}
